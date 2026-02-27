using UnityEngine;
using System.Collections.Generic;

// This class handles the deck as it is during the game. It is mutable over the course of the run, but is controlled by the DeckManager.
public class Deck
{
    private List<GameObject> deck;
    private GameObject tilePrefab;
    
    public Deck(GameObject tilePrefab)
    {
        this.tilePrefab = tilePrefab;
    }
    
    public void InitializeDeck()
    {
        deck = new List<GameObject>(DeckConstant.CreateDeck(tilePrefab));
        Shuffle();
    }

    public GameObject DrawTile()
    {
        if (deck.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return null;
        }
        GameObject drawnTile = deck[0];
        deck.RemoveAt(0);
        return drawnTile;
    }

    public void AddTile(GameObject tile)
    {
        deck.Add(tile);
    }

    public void AddTiles(List<GameObject> tiles)
    {
        deck.AddRange(tiles);
    }

    public void Shuffle()
    {
        if (deck == null)
        {
            deck = new List<GameObject>(DeckConstant.CreateDeck(tilePrefab));
        }
        List<GameObject> shuffledDeck = new List<GameObject>(deck);
        for (int i = shuffledDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject temp = shuffledDeck[i];
            shuffledDeck[i] = shuffledDeck[j];
            shuffledDeck[j] = temp;
        }
        deck = shuffledDeck;
    }

    public int GetDeckCount()
    {
        return deck?.Count ?? 0;
    }
}
