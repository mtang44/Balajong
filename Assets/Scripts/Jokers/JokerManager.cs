using UnityEngine;
using System;
using System.Collections.Generic;

public class JokerManager : MonoBehaviour
{
    public static JokerManager Instance { get; private set; }
    public Dictionary<string, Action> actionMap;
    public int currentJokers = 0;
    public int maxJokers = 5;

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
