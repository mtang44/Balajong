using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;

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
    List<MahjongTileData> discard;
    public List<GameObject> selectedTiles;
    
    // Property to access hand for sorting
    public List<GameObject> Hand => hand;
    
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
        discard = new List<MahjongTileData>();
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
    public void sortHand()
    {
        //Here we will sort the hand based on the tile types and values,
        //and update the positions of the gameObjects accordingly.
        //we'll start with just the positioning
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject tileGO = hand[i];
            tileGO.transform.localPosition = new Vector3((i * 0.25f) - ((hand.Count - 1) * 0.125f), -0.5f, 1.5f);
            tileGO.transform.localRotation = Quaternion.Euler(-20, 180, 0);
        }
    }   
    public bool drawTile()
    {
        if (hand.Count < HAND_SIZE)
        {
            MahjongTileData drawnTile = deck.DrawTile();
            if (drawnTile != null)
                tileToHand(drawnTile);
            else
                return false;
        }
        else
            return false;
        return true;
    }

    void tileToHand(MahjongTileData tileData)
    {
        if (hand.Count < HAND_SIZE)
        {
            GameObject tileObject = Instantiate(tilePrefab);
            tileObject.transform.SetParent(mainCamera.transform);
            tileObject.transform.localPosition = Vector3.zero;
            hand.Add(tileObject);
            tileObject.GetComponent<MahjongTileHolder>().SetTileData(tileData);
            tileObject.GetComponent<MahjongTileHolder>().OnValidate();
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

    public void selectedToDiscard()
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
            MahjongTileData tileData = tile.GetComponent<MahjongTileHolder>().TileData;
            if (tileData != null)
            {
                discard.Add(tileData);
            }
            discardTileAnimation(tile);
        }
        else
            Debug.LogError("Tile not in hand!");
    }
    public void discardTileAnimation(GameObject tile)
    {
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomY = Random.Range(-0.05f, 0.05f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        Vector3 randomDirection = new Vector3(randomX, randomY, randomZ).normalized;
        tile.AddComponent<Rigidbody>();
        tile.GetComponent<Rigidbody>().AddForce(randomDirection * 3f, ForceMode.Impulse);
        StartCoroutine(destroyAfterSeconds(tile, 1f));
    }
    IEnumerator destroyAfterSeconds(GameObject tile, float seconds)
    {
        // Here we will do the animation for discarding a tile, then destroy
        yield return new WaitForSeconds(seconds);
        Destroy(tile);
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
