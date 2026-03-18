using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public List<AudioClip> sounds = new List<AudioClip>();
    [SerializeField] private AudioSource audioSourcePrefab;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void playSound(AudioClip audioClip, float volume = 1f)
    {
        AudioSource audioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }
    public void playDrawSound()
    {
        AudioClip drawSound = sounds[Random.Range(0, 3)];
        playSound(drawSound);
    }
    public void playSmallScoreSound() { playSound(sounds[3]); }
    public void playMediumScoreSound() { playSound(sounds[4], 0.6f); }
    public void playBigScoreSound() { playSound(sounds[5], 0.4f); }
    public void playDiscardSound() { playSound(sounds[6]); }
    public void playMenuSound() { playSound(sounds[7]); }
    public void playKillSound() { playSound(sounds[8], 0.5f); }
    public void playZoomSound() { playSound(sounds[9], 2f); }
    public void playWhooshSound() { playSound(sounds[10], 0.2f); }
    public void playCoinSound() { playSound(sounds[11], 0.7f); }
    public void playClickSound() { playSound(sounds[12]); }
    public void playIncorrectSound() { playSound(sounds[13]); }
}
