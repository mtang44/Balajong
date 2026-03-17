using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class JokerManager : MonoBehaviour
{
    public static JokerManager Instance { get; private set; }
    public GameObject JokerUIPrefab;
    public GameObject JokerUIContainer;
    public List<string> jokers = new List<string>();
    public int maxJokers = 5;

    private List<string> startingJokers = new List<string>();
    public int startingMaxJokers;

    public int knightJokerBuff = 0;
    public int baggedJokerBuff = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            startingJokers = new List<string>(jokers);
            startingMaxJokers = maxJokers;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Start()
    {
        if (JokerHolderUI.Instance != null && JokerHolderUI.Instance.gameObject.transform.childCount > 0)
        {
            JokerUIContainer = JokerHolderUI.Instance.gameObject.transform.GetChild(0).gameObject;
            EnsureAllJokersDraggable();
            SyncJokerOrderFromUI();
        }
        StatsUpdater.Instance?.UpdateJokerCount();
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
        maxJokers = startingMaxJokers;
        StatsUpdater.Instance?.UpdateJokerCount();
    }
    public void AddJoker(string jokerName, string jokerCode, string jokerDescription, int price, Texture jokerTexture)
    {
        jokers.Add(jokerCode);
        GameObject jokerUI = Instantiate(JokerUIPrefab, JokerUIContainer.transform);
        EnsureJokerDraggable(jokerUI);
        jokerUI.GetComponentInChildren<RawImage>(true).texture = jokerTexture;
        TMP_Text[] foundTMPs = jokerUI.GetComponentsInChildren<TMP_Text>(true);
        foreach(TMP_Text currentTMP in foundTMPs)
        {
            if(currentTMP.name == "Joker Title")
            {
                currentTMP.text = jokerName;
            }
            else if(currentTMP.name == "Description TMP")
            {
                currentTMP.text = jokerDescription;
            }
            else
            {
                Debug.Log("Unrecognized TMP acquired"); 
            }
        }
        JokerSelect jokerSelect = jokerUI.GetComponent<JokerSelect>();
        jokerSelect.Initialize(jokerCode, jokerName, jokerDescription, price);
        StatsUpdater.Instance?.UpdateJokerCount();
    }

    public void SyncJokerOrderFromUI()
    {
        if (JokerUIContainer == null || jokers == null)
            return;

        List<string> orderedCodes = new List<string>();
        Transform containerTransform = JokerUIContainer.transform;
        for (int i = 0; i < containerTransform.childCount; i++)
        {
            JokerSelect jokerSelect = containerTransform.GetChild(i).GetComponent<JokerSelect>();
            if (jokerSelect == null || string.IsNullOrEmpty(jokerSelect.code))
                continue;

            orderedCodes.Add(jokerSelect.code);
        }

        if (orderedCodes.Count == 0)
            return;

        jokers.Clear();
        jokers.AddRange(orderedCodes);
    }

    private void EnsureAllJokersDraggable()
    {
        if (JokerUIContainer == null)
            return;

        Transform containerTransform = JokerUIContainer.transform;
        for (int i = 0; i < containerTransform.childCount; i++)
        {
            EnsureJokerDraggable(containerTransform.GetChild(i).gameObject);
        }
    }

    private static void EnsureJokerDraggable(GameObject jokerUI)
    {
        if (jokerUI == null)
            return;

        if (jokerUI.GetComponent<JokerDrag>() == null)
            jokerUI.AddComponent<JokerDrag>();

        if (jokerUI.GetComponent<CanvasGroup>() == null)
            jokerUI.AddComponent<CanvasGroup>();
    }

}
