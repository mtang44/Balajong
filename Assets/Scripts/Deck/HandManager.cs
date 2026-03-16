using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    private DeckManager deckManager;
    public static HandManager Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        deckManager = DeckManager.Instance;
    }

    // Sort hand by Type with priority:
    // Dragon, Wind, Dots, Bam, Crack, Flower, Season.
    // Within each type, tiles are sorted by their respective values descending (high to low).
    [ContextMenu("Sort by Type")]
    public void SortHandByType()
    {
        if (deckManager == null)
        {
            deckManager = DeckManager.Instance;
            if (deckManager == null)
            {
                Debug.LogWarning("DeckManager not found!");
                return;
            }
        }

        List<GameObject> hand = deckManager.Hand;
        if (hand == null || hand.Count == 0) return;

        hand.Sort((a, b) => CompareByType(a, b));
        deckManager.sortHand(); // Reposition tiles after sorting
        //Debug.Log("Hand sorted by Type");
    }

    // Sort hand by Value with priority groups:
    // Dragon first, then Wind, then Numbered tiles (descending), then Flower and Season.
    [ContextMenu("Sort by Value")]
    public void SortHandByValue()
    {
        if (deckManager == null)
        {
            deckManager = DeckManager.Instance;
            if (deckManager == null)
            {
                Debug.LogWarning("DeckManager not found!");
                return;
            }
        }

        List<GameObject> hand = deckManager.Hand;
        if (hand == null || hand.Count == 0) return;

        hand.Sort((a, b) => CompareByValue(a, b));
        deckManager.sortHand(); // Reposition tiles after sorting
        //Debug.Log("Hand sorted by Value");
    }

    // Compare two tiles by Type using custom group order:
    // Dragon, Wind, Dots, Bam, Crack, Flower, Season.
    // Within same type, sort by value descending.
    private int CompareByType(GameObject tileA, GameObject tileB)
    {
        MahjongTileData dataA = tileA.GetComponent<MahjongTileHolder>()?.TileData;
        MahjongTileData dataB = tileB.GetComponent<MahjongTileHolder>()?.TileData;

        if (dataA == null || dataB == null) return 0;

        int typeComparison = GetTypeSortOrder(dataA.TileType).CompareTo(GetTypeSortOrder(dataB.TileType));
        if (typeComparison != 0) return typeComparison;

        // Within same type, compare by value
        return CompareValueWithinType(dataA, dataB);
    }

    // Compare two tiles by Value using group order:
    // Dragon, Wind, Numbered, Flower, Season.
    private int CompareByValue(GameObject tileA, GameObject tileB)
    {
        MahjongTileData dataA = tileA.GetComponent<MahjongTileHolder>()?.TileData;
        MahjongTileData dataB = tileB.GetComponent<MahjongTileHolder>()?.TileData;

        if (dataA == null || dataB == null) return 0;

        int groupComparison = GetValueSortGroup(dataA.TileType).CompareTo(GetValueSortGroup(dataB.TileType));
        if (groupComparison != 0) return groupComparison;

        // Determine if tiles are numbered (Dots, Bam, Crack)
        bool aIsNumbered = IsNumberedTile(dataA.TileType);
        bool bIsNumbered = IsNumberedTile(dataB.TileType);

        if (aIsNumbered && bIsNumbered)
        {
            // Both numbered - compare by value descending (high values on left)
            int valueComparison = ((int)dataB.NumberedValue).CompareTo((int)dataA.NumberedValue);
            if (valueComparison != 0) return valueComparison;
            
            // Same value, sort by type (Dots, Bam, Crack)
            return ((int)dataA.TileType).CompareTo((int)dataB.TileType);
        }

        // Same non-numbered group - sort by value within type
        return CompareValueWithinType(dataA, dataB);
    }

    // Priority used by SortHandByValue:
    // Dragon (0), Wind (1), Numbered (2), Flower (3), Season (4)
    private int GetValueSortGroup(TileType type)
    {
        return type switch
        {
            TileType.Wind => 0,
            TileType.Dragon => 1,
            TileType.Dots or TileType.Bam or TileType.Crack => 2,
            TileType.Flower => 3,
            TileType.Season => 4,
            _ => 999
        };
    }

    // Priority used by SortHandByType:
    // Dragon (0), Wind (1), Dots (2), Bam (3), Crack (4), Flower (5), Season (6)
    private int GetTypeSortOrder(TileType type)
    {
        return type switch
        {
            TileType.Wind => 0,
            TileType.Dragon => 1,
            TileType.Dots => 2,
            TileType.Bam => 3,
            TileType.Crack => 4,
            TileType.Flower => 5,
            TileType.Season => 6,
            _ => 999
        };
    }

    // Check if tile type is a numbered tile (Dots, Bam, or Crack).
    private bool IsNumberedTile(TileType type)
    {
        return type == TileType.Dots || type == TileType.Bam || type == TileType.Crack;
    }

    // Compare values within the same tile type (descending - high values first).
    private int CompareValueWithinType(MahjongTileData dataA, MahjongTileData dataB)
    {
        return dataA.TileType switch
        {
            TileType.Dots or TileType.Bam or TileType.Crack =>
                ((int)dataB.NumberedValue).CompareTo((int)dataA.NumberedValue),
            TileType.Wind =>
                ((int)dataB.WindValue).CompareTo((int)dataA.WindValue),
            TileType.Dragon =>
                ((int)dataB.DragonValue).CompareTo((int)dataA.DragonValue),
            TileType.Flower =>
                ((int)dataB.FlowerValue).CompareTo((int)dataA.FlowerValue),
            TileType.Season =>
                ((int)dataB.SeasonValue).CompareTo((int)dataA.SeasonValue),
            _ => 0
        };
    }
}
