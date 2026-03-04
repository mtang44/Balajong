using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

// This class will manage the deck of tiles during the game, holding what is in the hand and in the wall.
public class DeckManager : MonoBehaviour
{
    //
    //I am doing the main camera stuff somewhat temporarily
    //
    Camera mainCamera;
    [SerializeField] GameObject tilePrefab;
    public int HAND_SIZE = 14;
    public int MAX_DISCARD_SELECTION = 5;
    public static DeckManager Instance;

    // Our hands! Deck is the wall, then the hand and discard.
    Deck deck;
    List<GameObject> hand;
    List<string> discard;
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
        deck = new Deck(tilePrefab);
        deck.InitializeDeck();
        hand = new List<GameObject>();
        discard = new List<string>();
        selectedTiles = new List<GameObject>();
        mainCamera = Camera.main;
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
        sortHand();
    }
    void sortHand()
    {
        //Here we will sort the hand based on the tile types and values,
        //and update the positions of the gameObjects accordingly.
        //we'll start with just the positioning
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject tileGO = hand[i];
            tileGO.transform.localPosition = new Vector3((i * 0.2f) - ((hand.Count - 1) * 0.1f), -0.2f, 1.5f);
        }
    }   
    public bool drawTile()
    {
        if (hand.Count < HAND_SIZE)
        {
            GameObject drawnTile = deck.DrawTile();
            if (drawnTile != null)
                tileToHand(drawnTile);
            else
                return false;
        }
        else
            return false;
        return true;
    }
    void tileToHand(GameObject tileObject)
    {
        if (hand.Count < HAND_SIZE)
        {
            tileObject.transform.SetParent(mainCamera.transform);
            tileObject.transform.localPosition = Vector3.zero;
            hand.Add(tileObject);
        }
        else
            Debug.LogError("Hand is already full!");
    }

    void handToDiscard()
    {
        discardTiles(hand);
        hand.Clear();
        selectedTiles.Clear();
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
            MahjongTileData tileData = tile.GetComponent<MahjongTileData>();
            if (tileData != null)
            {
                // Store as string format: "value+suit"
                discard.Add(tileData.GetTileString());
            }
            Object.Destroy(tile);
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
