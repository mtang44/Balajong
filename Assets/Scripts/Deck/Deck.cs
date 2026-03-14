using UnityEngine;
using System.Collections.Generic;

// This class handles the deck as it is during the game. It is mutable over the course of the run, but is controlled by the DeckManager.
public class Deck
{
    private List<MahjongTileData> tileData;
    private GameObject tilePrefab;
    
    public Deck(GameObject tilePrefab)
    {
        this.tilePrefab = tilePrefab;
    }
    
    public void InitializeDeck()
    {
        tileData = DeckConstant.CreateDeckData();
        Shuffle();
    }

    public MahjongTileData DrawTile()
    {
        if (tileData.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return null;
        }
        
        MahjongTileData tileDatum = tileData[0];
        tileData.RemoveAt(0);
        
        return tileDatum;
    }

    public void AddTile(MahjongTileData newTileData)
    {
        tileData.Add(newTileData);
    }

    // Inserts a tile at the front of the deck (drawn next).
    public void AddTileAtFront(MahjongTileData newTileData)
    {
        if (tileData == null)
            tileData = new List<MahjongTileData>();
        tileData.Insert(0, newTileData);
    }

    public void AddTiles(List<MahjongTileData> tiles)
    {
        tileData.AddRange(tiles);
    }

    public void Shuffle()
    {
        if (tileData == null)
        {
            tileData = new List<MahjongTileData>(DeckConstant.CreateDeckData());
        }
        
        List<MahjongTileData> shuffledDeck = new List<MahjongTileData>(tileData);
        for (int i = shuffledDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            MahjongTileData temp = shuffledDeck[i];
            shuffledDeck[i] = shuffledDeck[j];
            shuffledDeck[j] = temp;
        }
        tileData = shuffledDeck;
    }

    public int GetDeckCount()
    {
        return tileData?.Count ?? 0;
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
