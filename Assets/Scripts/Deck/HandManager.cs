using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    private DeckManager deckManager;

    void Start()
    {
        deckManager = DeckManager.Instance;
    }

    // Sort hand by Type in order: Dots, Bam, Crack, Wind, Dragon, Flower, Season.
    // Within each type, tiles are sorted by their respective values ascending.
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
        Debug.Log("Hand sorted by Type");
    }

    // Sort hand by Value. High numbered values on the left (descending).
    // Non-numbered tiles go on the right in order: Wind, Dragon, Flower, Season.
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
        Debug.Log("Hand sorted by Value");
    }

    // Compare two tiles by Type (Dots, Bam, Crack, Wind, Dragon, Flower, Season).
    // Within same type, sort by their respective values.
    private int CompareByType(GameObject tileA, GameObject tileB)
    {
        MahjongTileData dataA = tileA.GetComponent<MahjongTileHolder>()?.TileData;
        MahjongTileData dataB = tileB.GetComponent<MahjongTileHolder>()?.TileData;

        if (dataA == null || dataB == null) return 0;

        // Compare by type first (enum order: Dots, Bam, Crack, Wind, Dragon, Flower, Season)
        int typeComparison = ((int)dataA.TileType).CompareTo((int)dataB.TileType);
        if (typeComparison != 0) return typeComparison;

        // Within same type, compare by value
        return CompareValueWithinType(dataA, dataB);
    }

    // Compare two tiles by Value. High numbered values come first (left side).
    // Non-numbered tiles come after in order: Wind, Dragon, Flower, Season.
    private int CompareByValue(GameObject tileA, GameObject tileB)
    {
        MahjongTileData dataA = tileA.GetComponent<MahjongTileHolder>()?.TileData;
        MahjongTileData dataB = tileB.GetComponent<MahjongTileHolder>()?.TileData;

        if (dataA == null || dataB == null) return 0;

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
        else if (aIsNumbered)
        {
            return -1; // Numbered tiles come first (on left)
        }
        else if (bIsNumbered)
        {
            return 1; // Non-numbered tiles come after
        }
        else
        {
            // Both non-numbered - sort by type order (Wind, Dragon, Flower, Season)
            int typeComparison = GetNonNumberedTypeOrder(dataA.TileType).CompareTo(GetNonNumberedTypeOrder(dataB.TileType));
            if (typeComparison != 0) return typeComparison;
            
            // Same type, sort by value within that type
            return CompareValueWithinType(dataA, dataB);
        }
    }

    // Check if tile type is a numbered tile (Dots, Bam, or Crack).
    private bool IsNumberedTile(TileType type)
    {
        return type == TileType.Dots || type == TileType.Bam || type == TileType.Crack;
    }

    // Get the sort order for non-numbered tile types.
    // Order: Wind (0), Dragon (1), Flower (2), Season (3).
    private int GetNonNumberedTypeOrder(TileType type)
    {
        return type switch
        {
            TileType.Wind => 0,
            TileType.Dragon => 1,
            TileType.Flower => 2,
            TileType.Season => 3,
            _ => 999 // Unknown types go last
        };
    }

    // Compare values within the same tile type.
    private int CompareValueWithinType(MahjongTileData dataA, MahjongTileData dataB)
    {
        return dataA.TileType switch
        {
            TileType.Dots or TileType.Bam or TileType.Crack =>
                ((int)dataA.NumberedValue).CompareTo((int)dataB.NumberedValue),
            TileType.Wind =>
                ((int)dataA.WindValue).CompareTo((int)dataB.WindValue),
            TileType.Dragon =>
                ((int)dataA.DragonValue).CompareTo((int)dataB.DragonValue),
            TileType.Flower =>
                ((int)dataA.FlowerValue).CompareTo((int)dataB.FlowerValue),
            TileType.Season =>
                ((int)dataA.SeasonValue).CompareTo((int)dataB.SeasonValue),
            _ => 0
        };
    }
}
