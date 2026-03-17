using UnityEngine;
using System;

public class ASG_Perlin : MonoBehaviour
{
    AudioSource audioSource;
    bool active = true;
    public float speed = 0.02f;
    public float depth = 0.3f;
    public float baseVolume = 1f;
    
    float noiseTime;
    float clipLength;
    public float fadeInPercentage = 0.2f;
    public float fadeOutPercentage = 0.2f;
    float fadeGate = 1f; 
    public float smooth = 6f;
    

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        fadeGate = 0f;
        if (AudioStreamGenerator.Instance.perlinVolumeVariation)
            baseVolume = audioSource.volume;
    }

    void Update()
    {
        if (audioSource.clip == null) return;

        float clipLen = audioSource.clip.length;
        float t = audioSource.time;

        // "dying" means we're in the last X% of the clip
        bool dying = t >= clipLen * (1f - fadeOutPercentage);

        float fadeInSeconds = Mathf.Max(0.0001f, clipLen * fadeInPercentage);
        float fadeOutSeconds = Mathf.Max(0.0001f, clipLen * fadeOutPercentage);

        float gateTarget = dying ? 0f : 1f;
        float fadeSeconds = dying ? fadeOutSeconds : fadeInSeconds;

        fadeGate = Mathf.MoveTowards(fadeGate, gateTarget, Time.deltaTime / fadeSeconds);

        float vol = baseVolume;
        if (active)
        {
            noiseTime += Time.deltaTime * speed;
            float n = Mathf.PerlinNoise(noiseTime, 0.5f);
            vol = Mathf.Clamp01(baseVolume + (n - 0.5f) * 2f * depth);
        }

        float target = vol * fadeGate;

        float a = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        audioSource.volume = Mathf.Lerp(audioSource.volume, target, a);
    }
    public void SetActive(bool isActive)
    {
        active = isActive;
    }

}
