using UnityEngine;
using System.Collections.Generic;

// This class handles the deck as it is during the game. It is mutable over the course of the run, but is controlled by the DeckManager.
public class Deck
{
    private List<string> tileDataStrings;
    private GameObject tilePrefab;
    
    public Deck(GameObject tilePrefab)
    {
        this.tilePrefab = tilePrefab;
    }
    
    public void InitializeDeck()
    {
        tileDataStrings = new List<string>(DeckConstant.CreateDeckData());
        Shuffle();
    }

    public GameObject DrawTile()
    {
        if (tileDataStrings.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return null;
        }
        
        string tileDataString = tileDataStrings[0];
        tileDataStrings.RemoveAt(0);
        
        // Instantiate tile only when drawn
        GameObject tileObject = Object.Instantiate(tilePrefab);
        ConfigureTile(tileObject, tileDataString);
        
        return tileObject;
    }

    public void AddTile(string tileDataString)
    {
        tileDataStrings.Add(tileDataString);
    }

    public void AddTiles(List<string> tiles)
    {
        tileDataStrings.AddRange(tiles);
    }

    public void Shuffle()
    {
        if (tileDataStrings == null)
        {
            tileDataStrings = new List<string>(DeckConstant.CreateDeckData());
        }
        
        List<string> shuffledDeck = new List<string>(tileDataStrings);
        for (int i = shuffledDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = shuffledDeck[i];
            shuffledDeck[i] = shuffledDeck[j];
            shuffledDeck[j] = temp;
        }
        tileDataStrings = shuffledDeck;
    }

    public int GetDeckCount()
    {
        return tileDataStrings?.Count ?? 0;
    }
    
    private void ConfigureTile(GameObject tileObject, string tileDataString)
    {
        string value = tileDataString.Substring(0, tileDataString.Length - 1);
        string suit = tileDataString.Substring(tileDataString.Length - 1, 1);
        
        MahjongTileData tileData = tileObject.GetComponent<MahjongTileData>();
        if (tileData != null)
        {
            DeckConstant.ConfigureTileData(tileData, value, suit);
        }
    }
}
