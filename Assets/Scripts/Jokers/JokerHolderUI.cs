using UnityEngine;

public class JokerHolderUI : MonoBehaviour
{
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
    
    public void RemoveJoker(int index)
    {
        if (index < JokerManager.Instance.JokerUIContainer.transform.childCount)
        {
            Destroy(JokerManager.Instance.JokerUIContainer.transform.GetChild(index).gameObject);
        }
    }
}
