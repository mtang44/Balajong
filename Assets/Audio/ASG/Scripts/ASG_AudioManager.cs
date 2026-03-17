using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using System;

using Random = UnityEngine.Random;

public class ASG_AudioManager : MonoBehaviour
{
    public static ASG_AudioManager Instance;

    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource ambianceSourcePrefab;
    [SerializeField] AudioSource musicSourcePrefab;
    AudioSource musicSource;

    //Keys for PlayerPrefs
    public const string MASTER_PLAYER_PREFS = "ASG_MasterVolume";
    public const string MUSIC_PLAYER_PREFS = "ASG_MusicVolume";
    public const string AMBIANCE_PLAYER_PREFS = "ASG_AmbianceVolume";

    [Header("AudioList")]
    public List<AudioSource> audioSources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        LoadVolume();
    }
    void Start()
    {
        SetupChannels();
    }
    void SetupChannels()
    {
        for(int i = 0; i < AudioStreamGenerator.Instance.tracks; i++)
        {
            AudioSource addedSource = Instantiate(ambianceSourcePrefab, transform.position, Quaternion.identity, transform);
            audioSources.Add(addedSource);
            addedSource.GetComponent<ASG_Perlin>().SetActive(AudioStreamGenerator.Instance.perlinVolumeVariation);
            addedSource.GetComponent<ASG_Perlin>().depth = AudioStreamGenerator.Instance.volumeVariationRange;
            addedSource.GetComponent<ASG_Perlin>().fadeInPercentage = AudioStreamGenerator.Instance.fadeInPercentage;
            addedSource.GetComponent<ASG_Perlin>().fadeOutPercentage = AudioStreamGenerator.Instance.fadeOutPercentage;
        }
    }
    void LoadVolume()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_PLAYER_PREFS, ASG_VolumeManager.Instance.DefaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_PLAYER_PREFS, ASG_VolumeManager.Instance.DefaultMusicVolume);
        float ambianceVolume = PlayerPrefs.GetFloat(AMBIANCE_PLAYER_PREFS, ASG_VolumeManager.Instance.DefaultAmbianceVolume);

        mixer.SetFloat(ASG_VolumeManager.MIXER_MASTER, Mathf.Log10(masterVolume) * 20);
        mixer.SetFloat(ASG_VolumeManager.MIXER_MUSIC, Mathf.Log10(musicVolume) * 20);
        mixer.SetFloat(ASG_VolumeManager.MIXER_AMBIANCE, Mathf.Log10(ambianceVolume) * 20);
    }
    public void PlayAmbianceClip(AudioClip clip, float volume = 1f)
    {
        AudioSource audioSource = Instantiate(ambianceSourcePrefab, transform.position, Quaternion.identity);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }
    public void PlayAmbianceTrack(int trackIndex, AudioClip clip, float volume = 1f)
    {
        if (trackIndex < 0 || trackIndex >= audioSources.Count)
        {
            Debug.LogError($"ASG_AudioManager: Invalid track index {trackIndex}.", this);
            return;
        }

        AudioSource source = audioSources[trackIndex];
        source.clip = clip;
        source.volume = 0f;
        source.GetComponent<ASG_Perlin>().baseVolume = volume;
        source.Play();
        StartCoroutine(RequestNewTrack(trackIndex, clip.length));
    }
    IEnumerator RequestNewTrack(int trackIndex, float delay)
    {

        float randomDelay = Random.Range(-AudioStreamGenerator.Instance.randomDelayBetweenAmbianceRange, AudioStreamGenerator.Instance.randomDelayBetweenAmbianceRange);
        yield return new WaitForSeconds(delay + Math.Clamp(AudioStreamGenerator.Instance.delayBetweenAmbiance + randomDelay, 0f, float.MaxValue));
        AudioClip oldClip = audioSources[trackIndex].clip;  // Save OLD clip reference
        AudioStreamGenerator.Instance.PlayRandomAmbianceClip(trackIndex);
        AudioStreamGenerator.ClipEntry removedEntry = AudioStreamGenerator.Instance.usedClips.Find(entry => entry.clip == oldClip);
        if (removedEntry != null)
            AudioStreamGenerator.Instance.usedClips.Remove(removedEntry);
        AudioStreamGenerator.Instance.usableClips.Add(removedEntry);
    }
    public void StartMusicClip(AudioClip clip, float volume = 1f)
    {
        musicSource = Instantiate(musicSourcePrefab, transform.position, Quaternion.identity);
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }
    public void StopMusicClip()
    {
        Destroy(musicSource.gameObject);
    }
}
