#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ASG_AudioResourceLoader))]
public class ASG_AudioResourceLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var loader = (ASG_AudioResourceLoader)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Import Tools", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Import Music"))
                loader.ImportMusic();

            if (GUILayout.Button("Import Ambiance"))
                loader.ImportAmbiance();
        }

        if (GUILayout.Button("Import Both"))
            loader.ImportBoth();
    }
}
#endif