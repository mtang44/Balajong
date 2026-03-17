using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;

public class MutedMusic : MonoBehaviour
{
    public static MutedMusic Instance;
    public AudioMixer mixer;
    private Coroutine lowpassLerpCoroutine;
    private readonly float lerpDuration = 0.5f;

    private void Awake()
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
    void OnDisable()
    {
        // Unsubscribe from the event when the object is disabled
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene " + scene.name + " loaded with mode: " + mode);

        if (scene.buildIndex == 0) //This is the Title Screen
            unmuffleMusic();
        else if (scene.buildIndex == 1)
            muffleMusic();
        else if (scene.buildIndex == 2)
            unmuffleMusic();
        else if (scene.buildIndex == 3)
            muffleMusic();
        else
            muffleMusic();
    }
    void muffleMusic()
    {
        LerpLowpass(800);
    }
    void unmuffleMusic()
    {
        LerpLowpass(22000);
    }
    void LerpLowpass(float targetValue)
    {
        if (lowpassLerpCoroutine != null)
        {
            StopCoroutine(lowpassLerpCoroutine);
        }
        lowpassLerpCoroutine = StartCoroutine(LowpassLerpCoroutine(targetValue));
    }
    IEnumerator LowpassLerpCoroutine(float targetValue)
    {
        mixer.GetFloat("Lowpass", out float currentValue);
        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float newValue = Mathf.Lerp(currentValue, targetValue, elapsed / lerpDuration);
            mixer.SetFloat("Lowpass", newValue);
            yield return null;
        }

        mixer.SetFloat("Lowpass", targetValue);
    }
}
