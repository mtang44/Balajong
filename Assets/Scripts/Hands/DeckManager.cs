using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// This class will manage the deck of tiles during the game, holding what is in the hand and in the wall.
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    Deck deck;
    
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

    void Start()
    {
    }
    
}
