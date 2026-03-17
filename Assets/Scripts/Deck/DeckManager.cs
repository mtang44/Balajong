using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;

// This class will manage the deck of tiles during the game, holding what is in the hand and in the wall.
public class DeckManager : MonoBehaviour
{
    public GameObject TileHolder;

    private bool warnedMissingTileHolder = false;

    [SerializeField] GameObject tilePrefab;
    public int HAND_SIZE = 14;
    public int MAX_DISCARD_SELECTION = 5;
    public static DeckManager Instance;

    // Tiles queued for deal animation (populated during tileToHand, consumed after sort).
    private List<GameObject> pendingDealTiles = new List<GameObject>();
    private bool isDrawingHand = false;

    // Our hands! Deck is the wall, then the hand and discard.
    public Deck deck;
    public List<GameObject> hand = new List<GameObject>();
    public List<MahjongTileData> discard = new List<MahjongTileData>();
    public List<GameObject> selectedTiles = new List<GameObject>();
    public List<GameObject> flowerTiles = new List<GameObject>();
    public List<GameObject> seasonTiles = new List<GameObject>();
    
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
        deck = new Deck(tilePrefab);
        deck.InitializeDeck();
    }
    public void Start()
    {
        HAND_SIZE = 14;
        MAX_DISCARD_SELECTION = 5 + (3 * JokerManager.Instance.numberOfActivations("basket"));
    }

    public void forceNewLists()
    {
        hand = new List<GameObject>();
        selectedTiles = new List<GameObject>();
        flowerTiles = new List<GameObject>();
        seasonTiles = new List<GameObject>();
        discard = new List<MahjongTileData>();
    }

    public void PrepareForBattle()
    {
        forceNewLists();
        pendingDealTiles.Clear();
        isDrawingHand = false;

        if (deck == null)
        {
            deck = new Deck(tilePrefab);
            deck.InitializeDeck();
            return;
        }

        deck.Shuffle();
    }

    public void ResetToDefaultState()
    {
        DestroyTrackedTiles(hand);
        DestroyTrackedTiles(selectedTiles);
        DestroyTrackedTiles(flowerTiles);
        DestroyTrackedTiles(seasonTiles);

        forceNewLists();
        deck = new Deck(tilePrefab);
        deck.InitializeDeck();
    }

    private void DestroyTrackedTiles(List<GameObject> tiles)
    {
        if (tiles == null)
        {
            return;
        }

        foreach (GameObject tile in tiles)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
    }

    public void drawHand(int count = 0)
    {
        // Track recursion depth so we only trigger the deal animation once at the top level.
        bool isTopLevel = !isDrawingHand;
        isDrawingHand = true;

        HAND_SIZE = 14;
        MAX_DISCARD_SELECTION = 5 + (3 * JokerManager.Instance.numberOfActivations("basket"));
        if(count == 0) count = HAND_SIZE;
        for (int i = 0; i < count; i++)
        {
            bool success = drawTile();
            if (!success)
                break;
        }
        if(HandManager.Instance != null)
        {
            HandManager.Instance.SortHandByValue();
        } else
        {
            sortHand();
        }

        if (isTopLevel)
        {
            isDrawingHand = false;
            DrawVisualization.Instance?.AnimateDeal(pendingDealTiles);
            pendingDealTiles.Clear();
        }
    }
    public void sortHand()
    {
        //Here we will sort the hand based on the tile types and values,
        //and update the positions of the gameObjects accordingly.
        //we'll start with just the positioning
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject tileGO = hand[i];
            float baseY = -0.5f;
            
            // Preserve Y offset for selected tiles
            if (selectedTiles.Contains(tileGO))
            {
                baseY += 0.125f;
            }
            
            tileGO.transform.localPosition = new Vector3((i * 0.25f) - ((hand.Count - 1) * 0.125f), baseY, 1.5f);
            tileGO.transform.localRotation = Quaternion.Euler(-20, 180, 0);
        }
        cornerFlowers();
    }
    void cornerFlowers()
    {
        int tilesToDraw = 0;
        // Check for flower tiles in hand and move them to the flower area
        List<GameObject> flowersInHand = hand.FindAll(tile => tile.GetComponent<MahjongTileHolder>().TileData.TileType == TileType.Flower);
        List<GameObject> seasonsInHand = hand.FindAll(tile => tile.GetComponent<MahjongTileHolder>().TileData.TileType == TileType.Season);
        tilesToDraw += flowersInHand.Count;
        tilesToDraw += seasonsInHand.Count;
        foreach (GameObject flower in flowersInHand)
        {
            hand.Remove(flower);
            flowerTiles.Add(flower);
            // Move the flower tile to the designated flower area
            flower.transform.localPosition = new Vector3(-1.5f, -0.85f, 1.5f);
            flower.transform.localRotation = Quaternion.Euler(-20, 180, 0);
        }
        foreach (GameObject season in seasonsInHand)
        {
            hand.Remove(season);
            seasonTiles.Add(season);
            // Move the season tile to the designated season area
            season.transform.localPosition = new Vector3(1.5f, -0.85f, 1.5f);
            season.transform.localRotation = Quaternion.Euler(-20, 180, 0);
        }
        if(tilesToDraw > 0)
            drawHand(tilesToDraw);
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
            tileObject.transform.SetParent(GetTileParentTransform(), false);
            tileObject.transform.localPosition = Vector3.zero;
            hand.Add(tileObject);
            pendingDealTiles.Add(tileObject);
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

    public void selectedToDiscard(bool duplicating = false)
    {
        discardTiles(selectedTiles, duplicating);
        selectedTiles.Clear();
    }
    public void removeSelectedTiles()
    {
        foreach (GameObject tile in new List<GameObject>(selectedTiles))
        {
            hand.Remove(tile);
            discardTileAnimation(tile);
        }
        selectedTiles.Clear();
    }

    public void ClearSelectedTiles()
    {
        if (selectedTiles == null || selectedTiles.Count == 0)
        {
            return;
        }

        selectedTiles.Clear();
        sortHand();
    }

    public void fsToDiscard()
    {
        foreach (GameObject season in seasonTiles)
        {
            MahjongTileData tileData = season.GetComponent<MahjongTileHolder>().TileData;
            if (tileData != null)
                discard.Add(tileData);
            discardTileAnimation(season);
        }
        foreach (GameObject flower in flowerTiles)
        {
            MahjongTileData tileData = flower.GetComponent<MahjongTileHolder>().TileData;
            if (tileData != null)
                discard.Add(tileData);
            discardTileAnimation(flower);
        }
        seasonTiles.Clear();
        flowerTiles.Clear();
    }

    public void redrawHand()
    {
        int HAND_SIZE = 14;
        int MAX_DISCARD_SELECTION = 5 + (3 * JokerManager.Instance.numberOfActivations("basket"));
        int tilesToDraw = HAND_SIZE - hand.Count;
        if (tilesToDraw > 0)
            drawHand(tilesToDraw);
    }

    public void discardTile(GameObject tile, bool duplicating = false)
    {
        if (hand.Contains(tile))
        {
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("knight"); i++)
            {
                if(tile.GetComponent<MahjongTileHolder>().TileData.TileType == TileType.Dragon)
                     JokerManager.Instance.knightJokerBuff++;
            }
            hand.Remove(tile);
            MahjongTileData tileData = tile.GetComponent<MahjongTileHolder>().TileData;
            if (tileData != null)
            {
                discard.Add(tileData);
                if(duplicating)
                {
                    for(int i = 0; i < JokerManager.Instance.numberOfActivations("jackjack"); i++)
                    {
                        discard.Add(tileData);
                    }
                }
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
    public void discardTiles(List<GameObject> tiles, bool duplicating = false)
    {
        // Create a copy to avoid "Collection was modified" error when items are removed during iteration
        foreach (GameObject tile in new List<GameObject>(tiles))
        {
            discardTile(tile, duplicating);
        }
    }
    public void endRound()
    {
        // Move all hand tiles to discard pile
        handToDiscard();
        selectedToDiscard();
        fsToDiscard();

        // Return all tiles (both hand and discard) back to the deck
        deck.AddTiles(discard);
        discard = new List<MahjongTileData>();

        Debug.Log("Deck count after endRound: " + deck.GetDeckCount());
        deck.Shuffle();
    }

    public List<MahjongTileData> getHandAsMahjongTileData()
    {
        List<MahjongTileData> handData = new List<MahjongTileData>();
        foreach (GameObject tile in hand)
        {
            MahjongTileData tileData = tile.GetComponent<MahjongTileHolder>().TileData;
            if (tileData != null)
            {
                handData.Add(tileData);
            }
        }
        return handData;
    }

    private Transform GetTileParentTransform()
    {
        TileHolder = GameObject.FindWithTag("TileHolder");
        if (TileHolder != null)
            return TileHolder.transform;

        if (!warnedMissingTileHolder)
        {
            warnedMissingTileHolder = true;
            Debug.LogWarning("DeckManager: TileHolder is not assigned. Falling back to DeckManager transform.");
        }

        return transform;
    }
}
