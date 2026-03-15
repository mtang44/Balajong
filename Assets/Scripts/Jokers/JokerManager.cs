using UnityEngine;
using System;
using System.Collections.Generic;

public class JokerManager : MonoBehaviour
{
    public static JokerManager Instance { get; private set; }
    public List<string> jokers = new List<string>();
    public int currentJokers = 0;
    public int maxJokers = 5;

    private List<string> startingJokers = new List<string>();
    private int startingCurrentJokers;
    private int startingMaxJokers;

    public int knightJokerBuff = 0;
    public int baggedJokerBuff = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            startingJokers = new List<string>(jokers);
            startingCurrentJokers = currentJokers;
            startingMaxJokers = maxJokers;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int numberOfActivations(string jokerName)
    {
        if (jokers.Contains(jokerName))
        {
            if(jokers.IndexOf(jokerName) != 0 && jokers.IndexOf(jokerName) - 1 == jokers.IndexOf("simpson"))
            {
                return 2;
            }
            return 1;
        }
        return 0;
    }

    public void ResetRunState()
    {
        if (jokers == null)
        {
            jokers = new List<string>();
        }
        else
        {
            jokers.Clear();
        }

        jokers.AddRange(startingJokers);
        currentJokers = startingCurrentJokers;
        maxJokers = startingMaxJokers;
    }
}
