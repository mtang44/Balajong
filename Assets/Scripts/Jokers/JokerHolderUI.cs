using UnityEngine;

public class JokerHolderUI : MonoBehaviour
{
    //literally only for the singleton
    public static JokerHolderUI Instance { get; private set; }
    void Awake()
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
}
