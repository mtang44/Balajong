using UnityEngine;
using UnityEngine.Audio;

/*
    If you would like player control of the audio volumes:
        This class should be referenced by audio sliders. I recommend using a slider.
        Call the SetVolume functions with a float between 0 and 1.
            If you want, the upper bound can be the DefaultVolume variable.
            this way, the player can only increase volume to the starting level.
        Load the slider progress with the GetVolume functions.
        The lower bounds must be (somewhat close to) .0001 to avoid log(0) errors.
    Otherwise edit the DefaultVolume variables in the inspector to set starting volumes,
        once you have finalized their relative levels.
*/


public class ASG_VolumeManager : MonoBehaviour
{
    public static ASG_VolumeManager Instance;
    [SerializeField] AudioMixer mixer;

    // This section holds the default volume levels.
    // As a warning, changing this will NOT immediately affect the volume.
    // If you want a live update, use the audio mixer file
    // Set these once you are happy with the relative volumes of each stream
    [Header("Stream Default Volumes")]
    [Range(0.001f, 1.0f)]
    public float DefaultMasterVolume = 1f;

    [Range(0.001f, 1.0f)]
    public float DefaultMusicVolume = 1f;

    [Range(0.001f, 1.0f)]
    public float DefaultAmbianceVolume = 1f;

    // Gets the parameters for the various mixer groups
    public const string MIXER_MASTER = "MasterVolume";
    public const string MIXER_MUSIC = "MusicVolume";
    public const string MIXER_AMBIANCE = "AmbianceVolume";

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
    }


    // This function saves the volume settings when the object is disabled
    void OnDisable()
    {
        PlayerPrefs.SetFloat(ASG_AudioManager.MASTER_PLAYER_PREFS, GetMasterVolume());
        PlayerPrefs.SetFloat(ASG_AudioManager.MUSIC_PLAYER_PREFS, GetMusicVolume());
        PlayerPrefs.SetFloat(ASG_AudioManager.AMBIANCE_PLAYER_PREFS, GetAmbianceVolume());
    }

    // These functions set the volume based off a 0-1 float input and convert to decibels
    public void SetMasterVolume(float volume)
    {
        mixer.SetFloat(MIXER_MASTER, Mathf.Log10(volume) * 20);
    }
    public void SetMusicVolume(float volume)
    {
        mixer.SetFloat(MIXER_MUSIC, Mathf.Log10(volume) * 20);
    }
    public void SetAmbianceVolume(float volume)
    {
        mixer.SetFloat(MIXER_AMBIANCE, Mathf.Log10(volume) * 20);
    }

    // These functions get the current volume from the mixer and convert back to a 0-1 float
    public float GetMasterVolume()
    {
        float volume;
        mixer.GetFloat(MIXER_MASTER, out volume);
        return Mathf.Pow(10, volume / 20);
    }
    public float GetMusicVolume()
    {
        float volume;
        mixer.GetFloat(MIXER_MUSIC, out volume);
        return Mathf.Pow(10, volume / 20);
    }
    public float GetAmbianceVolume()
    {
        float volume;
        mixer.GetFloat(MIXER_AMBIANCE, out volume);
        return Mathf.Pow(10, volume / 20);
    }
}
