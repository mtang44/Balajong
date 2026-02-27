using UnityEngine;
using System.Collections.Generic;

// This class handles the deck as it is during the game. It is mutable over the course of the run, but is controlled by the DeckManager.
public class Deck
{
    private List<MahjongTile> deck;
    
    public void InitializeDeck()
    {
        deck = new List<MahjongTile>(DeckConstant.CreateDeck());
        Shuffle();
    }

    public MahjongTile DrawTile()
    {
        if (deck.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return null;
        }
        MahjongTile drawnTile = deck[0];
        deck.RemoveAt(0);
        return drawnTile;
    }

    public void AddTile(MahjongTile tile)
    {
        deck.Add(tile);
    }

    public void AddTiles(List<MahjongTile> tiles)
    {
        deck.AddRange(tiles);
    }

    public void Shuffle()
    {
        if (deck == null)
        {
            deck = new List<MahjongTile>(DeckConstant.CreateDeck());
        }
        List<MahjongTile> shuffledDeck = new List<MahjongTile>(deck);
        for (int i = shuffledDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            MahjongTile temp = shuffledDeck[i];
            shuffledDeck[i] = shuffledDeck[j];
            shuffledDeck[j] = temp;
        }
        deck = shuffledDeck;
    }
}
