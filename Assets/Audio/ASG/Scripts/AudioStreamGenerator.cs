using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AudioStreamGenerator : MonoBehaviour
{
    public static AudioStreamGenerator Instance;

    [Serializable]
    public class ClipEntry
    {
        public AudioClip clip;

        [Range(0f, 1.2f)]
        public float volume = 1f;

        [Tooltip("Higher = more likely to play.")]
        public int priority = 0;

        public override bool Equals(object obj)
        {
            if (obj is ClipEntry other)
            {
                return clip == other.clip;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return clip != null ? clip.GetHashCode() : 0;
        }
    }
    // Interface for ASG to access audio clips and their properties
    [Header("Settings")]
    [Tooltip("Number of simultaneous ambiance tracks to play.")]
    [SerializeField, Range(1, 8)] public int tracks = 2;

    [Header("Delay Control")]
    [Tooltip("Average delay between ambiance clips, in seconds.")]
    [SerializeField, Range(0f, 10f)] public float delayBetweenAmbiance = 5f;
    [Tooltip("Range of delay between ambiance clips, in seconds.")]
    [SerializeField, Range(0f, 10f)] public float randomDelayBetweenAmbianceRange = 2f;

    [Header("Fade Control")]
    [Tooltip("Percentage of the clip length for the fade-in effect.")]
    [SerializeField, Range(0f, 1f)] public float fadeInPercentage = 0.2f;
    [Tooltip("Percentage of the clip length for the fade-out effect.")]
    [SerializeField, Range(0f, 1f)] public float fadeOutPercentage = 0.2f;


    [Header("Perlin Noise Settings")]
    [Tooltip("If enabled, uses Perlin noise to vary volume within the clip.")]
    [SerializeField] public bool perlinVolumeVariation = true;

    [Tooltip("Variation applied to ambiance clip volume. 0 is no variation, 1 is significant volume up or down.")]
    [SerializeField, Range(0f, 1f)] public float volumeVariationRange = 0.3f;

    [Header("Audio Clips")]
    public List<ClipEntry> ambianceClips;
    public List<ClipEntry> musicClips;

    [Header("Mid-Game Control")]
    public List<ClipEntry> usableClips = new List<ClipEntry>();
    public List<ClipEntry> usedClips = new List<ClipEntry>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        int randomMusic = Random.Range(0, musicClips.Count);
        ASG_AudioManager.Instance.StartMusicClip(musicClips[randomMusic].clip, 1f);
        usableClips.AddRange(ambianceClips);
        StartCoroutine(BeginPlay(1f, 1f));
    }
    IEnumerator BeginPlay(float initialDelay, float volume)
    {
        yield return new WaitForSeconds(initialDelay);
        for (int i = 0; i < tracks; i++)
        {
            float delay = PlayRandomAmbianceClip(i, volume);
            yield return new WaitForSeconds(delay / 2);
        }
    }
    public float PlayRandomAmbianceClip(int trackIndex = 0, float volume = 1f)
    {
        if (usableClips.Count == 0)
        {
            Debug.LogWarning("AudioStreamGenerator: No usable ambiance clips available to play.", this);
            return 0f;
        }

        // Select a clip based on priority weights
        int totalPriority = 0;
        foreach (var entry in usableClips)
        {
            totalPriority += entry.priority;
        }

        int randomValue = Random.Range(0, totalPriority);
        AudioClip selectedClip = null;
        float selectedVolume = 1f;

        foreach (var entry in usableClips)
        {
            if (randomValue < entry.priority)
            {
                selectedClip = entry.clip;
                selectedVolume = entry.volume;
                usableClips.Remove(entry);
                usedClips.Add(entry);
                break;
            }
            randomValue -= entry.priority;
        }

        if (selectedClip != null)
        {
            ASG_AudioManager.Instance.PlayAmbianceTrack(trackIndex, selectedClip, volume * selectedVolume);
            return selectedClip.length;
        }
        return 0f;
    }
}
