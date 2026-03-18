using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ASG_AudioResourceLoader : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] AudioStreamGenerator targetGenerator;

    [Header("Resource Folders")]
    [Tooltip("Example: Assets/Audio/ASG/Music")]
    [SerializeField] private string musicFolder = "Assets";
    [Tooltip("Example: Assets/Audio/ASG/Ambiance")]
    [SerializeField] private string ambianceFolder = "Assets";
    [SerializeField] private bool includeSubfolders = false;

    private string folderPath;


    [Header("Defaults for imported clips")]
    [Tooltip("Priority is how likely the clip is to randomly play. Higher priority clips play more often.")]
    [SerializeField] private int defaultPriority = 1;
    [Tooltip("Volume is a multiplier applied to the clip's volume. 1 = no change. I recommend increasing over 1 rarely.")]
    [SerializeField, Range(0f, 1.2f)] private float defaultVolume = 1f;

    [Header("Import Behavior")]
    [Tooltip("If enabled, clears the target AudioStreamGenerator's clip list before adding new clips.")]
    [SerializeField] private bool clearTargetListFirst = true;
    [Tooltip("If enabled, skips importing clips that are already in the target list (by AudioClip reference).")]
    [SerializeField] private bool skipDuplicatesByClip = true;

    [Header("Parse Metadata from Filename")]
    [Tooltip("If enabled, parses tokens like: explosion__v0.8__p5 (volume=0.8, priority=5)")]
    [SerializeField] private bool parseFromFilename = false;

// Note to self - much of this section is programmed by ChatGPT. Use with caution.
#if UNITY_EDITOR
    public void ImportMusic()    => ImportInto(isMusic: true);
    public void ImportAmbiance() => ImportInto(isMusic: false);

    public void ImportBoth()
    {
        ImportInto(isMusic: true);
        ImportInto(isMusic: false);
    }

    private void ImportInto(bool isMusic)
    {
        if (targetGenerator == null)
        {
            Debug.LogError($"{nameof(ASG_AudioResourceLoader)}: Target generator is not set.", this);
            return;
        }

        string folderPath = isMusic ? musicFolder : ambianceFolder;
        string label = isMusic ? "Music" : "Ambiance";

        if (string.IsNullOrWhiteSpace(folderPath) || !folderPath.StartsWith("Assets", StringComparison.Ordinal))
        {
            Debug.LogError($"{nameof(ASG_AudioResourceLoader)}: {label} folder must be project-relative and start with 'Assets/'.", this);
            return;
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"{nameof(ASG_AudioResourceLoader)}: {label} folder does not exist: {folderPath}", this);
            return;
        }

        // Pick the destination list
        List<AudioStreamGenerator.ClipEntry> list =
            isMusic ? targetGenerator.musicClips : targetGenerator.ambianceClips;

        Undo.RecordObject(targetGenerator, $"Import {label} Audio Clips");

        if (clearTargetListFirst)
            list.Clear();

        // Build a quick duplicate check
        HashSet<AudioClip> existing = null;
        if (skipDuplicatesByClip)
        {
            existing = new HashSet<AudioClip>();
            foreach (var e in list)
                if (e != null && e.clip != null)
                    existing.Add(e.clip);
        }

        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        int added = 0;
        int skipped = 0;

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (!includeSubfolders)
            {
                // only accept assets directly in folderPath
                var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (!string.Equals(dir, folderPath, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null) continue;

            if (skipDuplicatesByClip && existing != null && existing.Contains(clip))
            {
                skipped++;
                continue;
            }

            var entry = new AudioStreamGenerator.ClipEntry
            {
                clip = clip,
                volume = defaultVolume,
                priority = defaultPriority
            };

            if (parseFromFilename)
                ApplyFilenameMetadata(clip.name, ref entry);

            list.Add(entry);
            existing?.Add(clip);
            added++;
        }

        EditorUtility.SetDirty(targetGenerator);
        Debug.Log($"[{nameof(ASG_AudioResourceLoader)}] Imported {added} {label} clips into '{targetGenerator.name}'. Skipped {skipped}.", this);
    }

    private static void ApplyFilenameMetadata(string clipName, ref AudioStreamGenerator.ClipEntry entry)
    {
        // Convention: name__v0.8__p5 (tokens separated by "__")
        var tokens = clipName.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            if (t.Length < 2) continue;

            if (t[0] == 'v' &&
                float.TryParse(t.Substring(1),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var vol))
            {
                entry.volume = Mathf.Clamp(vol, 0f, 1.2f);
            }
            else if (t[0] == 'p' && int.TryParse(t.Substring(1), out var pr))
            {
                entry.priority = pr;
            }
        }
    }
#endif
}
