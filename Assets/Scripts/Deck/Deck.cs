using UnityEngine;
using System.Collections.Generic;

// This class handles the deck as it is during the game. It is mutable over the course of the run, but is controlled by the DeckManager.
public class Deck
{
    private List<MahjongTile> deck;
    
    void InitializeDeck()
    {
        deck = new List<MahjongTile>(DeckConstant.CreateDeck());
        Shuffle();
    }

    MahjongTile DrawTile()
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

    List<MahjongTile> DrawTiles(int count)
    {
        List<MahjongTile> drawnTiles = new List<MahjongTile>();
        for (int i = 0; i < count; i++)
        {
            MahjongTile tile = DrawTile();
            if (tile != null)
            {
                drawnTiles.Add(tile);
            }
            else
            {
                break;
            }
        }
        return drawnTiles;
    }

    void AddTile(MahjongTile tile)
    {
        deck.Add(tile);
    }

    void AddTiles(List<MahjongTile> tiles)
    {
        deck.AddRange(tiles);
    }

    void Shuffle()
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
