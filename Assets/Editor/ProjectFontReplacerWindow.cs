using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProjectFontReplacerWindow : EditorWindow
{
    private TMP_FontAsset sourceTmpFont;
    private TMP_FontAsset targetTmpFont;
    private Font sourceLegacyFont;
    private Font targetLegacyFont;

    private bool replaceTmpFonts = true;
    private bool replaceLegacyFonts = true;
    private bool onlyReplaceMatchingSource = false;

    private struct ReplacementStats
    {
        public int assetsScanned;
        public int assetsChanged;
        public int componentsChanged;
    }

    [MenuItem("Tools/Fonts/Project Font Replacer")]
    public static void ShowWindow()
    {
        ProjectFontReplacerWindow window = GetWindow<ProjectFontReplacerWindow>("Project Font Replacer");
        window.minSize = new Vector2(420f, 280f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Replace Fonts Across Project", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This updates TextMeshPro and/or legacy UI Text fonts in all prefabs and scenes under Assets.",
            MessageType.Info);

        EditorGUILayout.Space();
        replaceTmpFonts = EditorGUILayout.ToggleLeft("Replace TextMeshPro Fonts", replaceTmpFonts);
        using (new EditorGUI.DisabledScope(!replaceTmpFonts))
        {
            sourceTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Source TMP Font", sourceTmpFont, typeof(TMP_FontAsset), false);
            targetTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target TMP Font", targetTmpFont, typeof(TMP_FontAsset), false);
        }

        EditorGUILayout.Space();
        replaceLegacyFonts = EditorGUILayout.ToggleLeft("Replace Legacy UI Text Fonts", replaceLegacyFonts);
        using (new EditorGUI.DisabledScope(!replaceLegacyFonts))
        {
            sourceLegacyFont = (Font)EditorGUILayout.ObjectField("Source UI Font", sourceLegacyFont, typeof(Font), false);
            targetLegacyFont = (Font)EditorGUILayout.ObjectField("Target UI Font", targetLegacyFont, typeof(Font), false);
        }

        EditorGUILayout.Space();
        onlyReplaceMatchingSource = EditorGUILayout.ToggleLeft("Only replace components using Source font", onlyReplaceMatchingSource);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanRunReplace()))
        {
            if (GUILayout.Button("Replace In All Prefabs + Scenes", GUILayout.Height(34f)))
            {
                ReplaceAcrossProject();
            }
        }

        if (!CanRunReplace())
        {
            EditorGUILayout.HelpBox(GetValidationMessage(), MessageType.Warning);
        }
    }

    private bool CanRunReplace()
    {
        if (!replaceTmpFonts && !replaceLegacyFonts)
        {
            return false;
        }

        if (replaceTmpFonts && targetTmpFont == null)
        {
            return false;
        }

        if (replaceLegacyFonts && targetLegacyFont == null)
        {
            return false;
        }

        if (!onlyReplaceMatchingSource)
        {
            return true;
        }

        bool hasTmpSource = !replaceTmpFonts || sourceTmpFont != null;
        bool hasLegacySource = !replaceLegacyFonts || sourceLegacyFont != null;
        return hasTmpSource && hasLegacySource;
    }

    private string GetValidationMessage()
    {
        if (!replaceTmpFonts && !replaceLegacyFonts)
        {
            return "Enable at least one replacement type.";
        }

        if (replaceTmpFonts && targetTmpFont == null)
        {
            return "Assign Target TMP Font to replace TextMeshPro text.";
        }

        if (replaceLegacyFonts && targetLegacyFont == null)
        {
            return "Assign Target UI Font to replace legacy UI Text.";
        }

        if (onlyReplaceMatchingSource)
        {
            if (replaceTmpFonts && sourceTmpFont == null)
            {
                return "Assign Source TMP Font, or disable the source-only option.";
            }

            if (replaceLegacyFonts && sourceLegacyFont == null)
            {
                return "Assign Source UI Font, or disable the source-only option.";
            }
        }

        return "Invalid configuration.";
    }

    private void ReplaceAcrossProject()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Replace Fonts Across Project",
                "This will modify and save prefabs and scenes under Assets. Continue?",
                "Replace",
                "Cancel"))
        {
            return;
        }

        ReplacementStats prefabStats = ReplaceFontsInPrefabs();
        ReplacementStats sceneStats = ReplaceFontsInScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int totalAssetsScanned = prefabStats.assetsScanned + sceneStats.assetsScanned;
        int totalAssetsChanged = prefabStats.assetsChanged + sceneStats.assetsChanged;
        int totalComponentsChanged = prefabStats.componentsChanged + sceneStats.componentsChanged;

        EditorUtility.DisplayDialog(
            "Font Replacement Complete",
            $"Scanned {totalAssetsScanned} assets.\nChanged {totalAssetsChanged} assets.\nUpdated {totalComponentsChanged} text components.",
            "OK");
    }

    private ReplacementStats ReplaceFontsInPrefabs()
    {
        ReplacementStats stats = new ReplacementStats();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            stats.assetsScanned++;

            if (EditorUtility.DisplayCancelableProgressBar("Replacing Fonts", $"Prefab: {path}", (float)i / Mathf.Max(1, prefabGuids.Length)))
            {
                break;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            int changedCount = ReplaceFontsInHierarchy(prefabRoot);

            if (changedCount > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                stats.assetsChanged++;
                stats.componentsChanged += changedCount;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        EditorUtility.ClearProgressBar();
        return stats;
    }

    private ReplacementStats ReplaceFontsInScenes()
    {
        ReplacementStats stats = new ReplacementStats();
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                stats.assetsScanned++;

                if (EditorUtility.DisplayCancelableProgressBar("Replacing Fonts", $"Scene: {path}", (float)i / Mathf.Max(1, sceneGuids.Length)))
                {
                    break;
                }

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                int changedCount = 0;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    changedCount += ReplaceFontsInHierarchy(roots[rootIndex]);
                }

                if (changedCount > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    stats.assetsChanged++;
                    stats.componentsChanged += changedCount;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }

        return stats;
    }

    private int ReplaceFontsInHierarchy(GameObject root)
    {
        int changedCount = 0;

        if (replaceTmpFonts)
        {
            TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (!ShouldReplaceTmpFont(text.font))
                {
                    continue;
                }

                text.font = targetTmpFont;
                EditorUtility.SetDirty(text);
                changedCount++;
            }
        }

        if (replaceLegacyFonts)
        {
            Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (!ShouldReplaceLegacyFont(text.font))
                {
                    continue;
                }

                text.font = targetLegacyFont;
                EditorUtility.SetDirty(text);
                changedCount++;
            }
        }

        return changedCount;
    }

    private bool ShouldReplaceTmpFont(TMP_FontAsset currentFont)
    {
        if (currentFont == targetTmpFont)
        {
            return false;
        }

        if (!onlyReplaceMatchingSource)
        {
            return true;
        }

        return currentFont == sourceTmpFont;
    }

    private bool ShouldReplaceLegacyFont(Font currentFont)
    {
        if (currentFont == targetLegacyFont)
        {
            return false;
        }

        if (!onlyReplaceMatchingSource)
        {
            return true;
        }

        return currentFont == sourceLegacyFont;
    }
}