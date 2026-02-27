using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// This class will manage the deck of tiles during the game, holding what is in the hand and in the wall.
public class DeckManager : MonoBehaviour
{
    public int HAND_SIZE = 14;
    public int MAX_DISCARD_SELECTION = 5;
    public static DeckManager Instance;

    // Our hands! Deck is the wall, then the hand and discard.
    Deck deck;
    List<GameObject> hand;
    List<MahjongTile> discard;
    public List<GameObject> selectedTiles;
    
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
        deck = new Deck();
        deck.InitializeDeck();
        hand = new List<GameObject>();
        discard = new List<MahjongTile>();
        selectedTiles = new List<GameObject>();
    }

    public void drawHand(int count = 0)
    {
        if(count == 0) count = HAND_SIZE;
        for (int i = 0; i < count; i++)
        {
            bool success = drawTile();
            if (!success)
                break;
        }
    }
    public bool drawTile()
    {
        if (hand.Count < HAND_SIZE)
        {
            MahjongTile drawnTile = deck.DrawTile();
            if (drawnTile != null)
                tileToHand(drawnTile);
            else
                return false;
        }
        else
            return false;
        return true;
    }
    void tileToHand(MahjongTile tile)
    {
        if (hand.Count < HAND_SIZE)
        {
            //Here we instantiate the tile and add the gameObject to the hand
        }
        else
            Debug.LogError("Hand is already full!");
    }

    void handToDiscard()
    {
        discardTiles(hand);
        hand.Clear();
    }

    void selectedToDiscard()
    {
        discardTiles(selectedTiles);
        selectedTiles.Clear();
    }

    public void redrawHand()
    {
        int tilesToDraw = HAND_SIZE - hand.Count;
        if (tilesToDraw > 0)
            drawHand(tilesToDraw);
    }

    public void discardTile(GameObject tile)
    {
        if (hand.Contains(tile))
        {
            hand.Remove(tile);
            discard.Add(tile.GetComponent<MahjongTile>());
        }
        else
            Debug.LogError("Tile not in hand!");
    }
    public void discardTiles(List<GameObject> tiles)
    {
        foreach (GameObject tile in tiles)
        {
            discardTile(tile);
        }
    }
    public void endRound()
    {
        handToDiscard();
        deck.AddTiles(discard);
        discard.Clear();
    }

}
