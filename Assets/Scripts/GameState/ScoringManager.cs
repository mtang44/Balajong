/* ScoringManager is a singleton class that will handle the scoring of the game.

Main functions:
- GetTileScore(tile): Returns the configured score value for a single tile (by type).
- CalcHandScore(tiles): Total score for a hand using tile values and meld formulas.
- HandMeetsThreshold(tiles, checkpointThreshold): True if hand meets or exceeds threshold.
- CalcDamage(tiles, checkpointThreshold): Amount hand falls short of threshold (0 if passed).
- CalcReward(tiles, checkpointThreshold): Amount hand beats threshold by (0 if not).
- DetectMelds(tiles): Returns all chows, pungs, and eyes (pairs) as Meld objects (with tile references).
- CalcAllMeldsScore(melds): Total score of all melds (uses individual tile values).
- EvalMeld(meld): Score for a single meld (sum of GetTileScore for each tile).

Tile values are stored per type (all initialized to 0); assign actual scores in Inspector or code.

Example usage with deck:
- int score = ScoringManager.Instance.CalcHandScore(DeckManager.Instance.hand);
- bool passed = ScoringManager.Instance.HandMeetsThreshold(DeckManager.Instance.hand, checkpointThreshold);
- int damage = ScoringManager.Instance.CalcDamage(DeckManager.Instance.hand, checkpointThreshold);
- int reward = ScoringManager.Instance.CalcReward(DeckManager.Instance.hand, checkpointThreshold);
- List<ScoringManager.Meld> melds = ScoringManager.Instance.DetectMelds(DeckManager.Instance.hand);
- int meldScore = ScoringManager.Instance.CalcAllMeldsScore(melds);
- int singleMeldScore = ScoringManager.Instance.EvalMeld(melds[0]);
*/

using System.Collections.Generic;
using UnityEngine;

// Alias so callers can pass MahjongTileData lists.
using MahjongTile = MahjongTileData;

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance;

    [SerializeField]
    private bool logScoringDetails = false;

    #region Tile values by type (all 0; set from a separate file that defines scores per TileType)
    // Assigning scores should look like this:
    // ScoringManager.Instance.dotsValues[1] = 10;
    // ScoringManager.Instance.bamValues[2] = 20;
    // ScoringManager.Instance.crackValues[3] = 30;
    // ScoringManager.Instance.windValues[0] = 40;
    // ScoringManager.Instance.dragonValues[1] = 50;
    // ScoringManager.Instance.flowerValues[2] = 60;
    // ScoringManager.Instance.seasonValues[3] = 70;

    // Suited: Dots, Bam, Crack each 1-9 (index 0 unused, 1-9 used)
    public int[] dotsValues = new int[10];
    public int[] bamValues = new int[10];
    public int[] crackValues = new int[10];
    // Winds: N E S W (index 0-3)
    public int[] windValues = new int[4];
    // Dragons: R G W (index 0-2)
    public int[] dragonValues = new int[3];
    // Flowers: Plum Orchid Bamboo Chrysanthemum (index 0-3)
    public int[] flowerValues = new int[4];
    // Seasons: Spring Summer Autumn Winter (index 0-3)
    public int[] seasonValues = new int[4];

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Initialize tile scores from the example score table so
            // all tiles have non-zero values by default.
            ScoreTable.ApplyDefaultScores(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Returns the configured score value for this tile (by type). Used for individual tile scoring and meld eval.
    public int GetTileScore(MahjongTile tile)
    {
        if (tile == null) return 0;

        switch (tile.TileType)
        {
            case TileType.Dots:
                return GetSuitedValue(dotsValues, (int)tile.NumberedValue);
            case TileType.Bam:
                return GetSuitedValue(bamValues, (int)tile.NumberedValue);
            case TileType.Crack:
                return GetSuitedValue(crackValues, (int)tile.NumberedValue);
            case TileType.Wind:
                return GetWindValue(tile.WindValue);
            case TileType.Dragon:
                return GetDragonValue(tile.DragonValue);
            case TileType.Flower:
                return GetFlowerValue(tile.FlowerValue);
            case TileType.Season:
                return GetSeasonValue(tile.SeasonValue);
            default:
                return 0;
        }
    }

    private static int GetSuitedValue(int[] arr, int value)
    {
        if (value >= 1 && value <= 9 && arr != null && arr.Length > value) return arr[value];
        return 0;
    }

    private int GetWindValue(WindValue w)
    {
        int i = w switch { WindValue.North => 0, WindValue.East => 1, WindValue.South => 2, WindValue.West => 3, _ => -1 };
        return (i >= 0 && windValues != null && i < windValues.Length) ? windValues[i] : 0;
    }

    private int GetDragonValue(DragonValue d)
    {
        int i = d switch { DragonValue.Red => 0, DragonValue.Green => 1, DragonValue.White => 2, _ => -1 };
        return (i >= 0 && dragonValues != null && i < dragonValues.Length) ? dragonValues[i] : 0;
    }

    private int GetFlowerValue(FlowerValue f)
    {
        int i = (int)f;
        return (flowerValues != null && i >= 0 && i < flowerValues.Length) ? flowerValues[i] : 0;
    }

    private int GetSeasonValue(SeasonValue s)
    {
        int i = (int)s;
        return (seasonValues != null && i >= 0 && i < seasonValues.Length) ? seasonValues[i] : 0;
    }

    #region Main scoring API (use tile values and meld calc/eval)

    // Calculates the total score for a hand of tiles using tile values and meld formulas.
    public int CalcHandScore(IReadOnlyList<MahjongTile> tiles)
    {
        if (tiles == null || tiles.Count == 0) return 0;

        List<Meld> melds = DetectMelds(tiles);
        int meldScore = CalcAllMeldsScore(melds);
        int bonusScore = 0;
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            if (IsBonusTile(tile) && !IsTileInAnyMeld(tile, melds))
                bonusScore += GetTileScore(tile);
        }

        int total = meldScore + bonusScore;
        if (logScoringDetails)
            Debug.Log($"Hand score: total={total}, melds={meldScore}, bonus={bonusScore}");
        return total;
    }

    // Checks if the hand meets the checkpoint threshold.
    public bool HandMeetsThreshold(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        return CalcHandScore(tiles) >= checkpointThreshold;
    }

    // Calculates the amount by which the hand falls short of the threshold.
    public int CalcDamage(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        int score = CalcHandScore(tiles);
        return score >= checkpointThreshold ? 0 : checkpointThreshold - score;
    }

    // Calculates the reward for a hand that beats the threshold.
    public int CalcReward(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        int score = CalcHandScore(tiles);
        return score > checkpointThreshold ? score - checkpointThreshold : 0;
    }

    #endregion

    #region Meld type and detection

    public enum MeldKind { Chow, Pung, Eyes }

    // Meld struct for storing the kind and tiles of a meld.
    public struct Meld
    {
        public MeldKind Kind;
        public List<MahjongTile> Tiles; // typically 3

        public Meld(MeldKind kind, List<MahjongTile> tiles)
        {
            Kind = kind;
            Tiles = tiles ?? new List<MahjongTile>();
        }
    }

    // Detects all chows and pungs in the hand. Returns meld objects that reference the actual tiles.
    public List<Meld> DetectMelds(IReadOnlyList<MahjongTile> tiles)
    {
        var melds = new List<Meld>();
        if (tiles == null || tiles.Count == 0) return melds;

        var suited = new Dictionary<TileType, List<MahjongTile>[]>
        {
            { TileType.Dots, new List<MahjongTile>[10] },
            { TileType.Bam, new List<MahjongTile>[10] },
            { TileType.Crack, new List<MahjongTile>[10] }
        };
        for (int i = 0; i < 10; i++)
        {
            suited[TileType.Dots][i] = new List<MahjongTile>();
            suited[TileType.Bam][i] = new List<MahjongTile>();
            suited[TileType.Crack][i] = new List<MahjongTile>();
        }
        var honorLists = new Dictionary<string, List<MahjongTile>>();

        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            switch (tile.TileType)
            {
                case TileType.Dots:
                case TileType.Bam:
                case TileType.Crack:
                    int val = (int)tile.NumberedValue;
                    if (val >= 1 && val <= 9)
                        suited[tile.TileType][val].Add(tile);
                    break;
                case TileType.Wind:
                case TileType.Dragon:
                    string key = GetHonorKey(tile);
                    if (!honorLists.ContainsKey(key)) honorLists[key] = new List<MahjongTile>();
                    honorLists[key].Add(tile);
                    break;
            }
        }

        // Extract chows (low to high) then pungs
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 7; v++)
            {
                var low = suited[suit][v];
                var mid = suited[suit][v + 1];
                var high = suited[suit][v + 2];
                while (low.Count > 0 && mid.Count > 0 && high.Count > 0)
                {
                    var chowTiles = new List<MahjongTile> { low[0], mid[0], high[0] };
                    low.RemoveAt(0); mid.RemoveAt(0); high.RemoveAt(0);
                    melds.Add(new Meld(MeldKind.Chow, chowTiles));
                }
            }
        }

        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 9; v++)
            {
                var list = suited[suit][v];
                while (list.Count >= 3)
                {
                    var pungTiles = new List<MahjongTile> { list[0], list[1], list[2] };
                    list.RemoveRange(0, 3);
                    melds.Add(new Meld(MeldKind.Pung, pungTiles));
                }
            }
        }

        foreach (var kvp in honorLists)
        {
            var list = kvp.Value;
            while (list.Count >= 3)
            {
                var pungTiles = new List<MahjongTile> { list[0], list[1], list[2] };
                list.RemoveRange(0, 3);
                melds.Add(new Meld(MeldKind.Pung, pungTiles));
            }
        }

        // Extract eyes (pairs): two identical tiles, score = (tile1 + tile2) * 2
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 9; v++)
            {
                var list = suited[suit][v];
                while (list.Count >= 2)
                {
                    var eyesTiles = new List<MahjongTile> { list[0], list[1] };
                    list.RemoveRange(0, 2);
                    melds.Add(new Meld(MeldKind.Eyes, eyesTiles));
                }
            }
        }

        foreach (var kvp in honorLists)
        {
            var list = kvp.Value;
            while (list.Count >= 2)
            {
                var eyesTiles = new List<MahjongTile> { list[0], list[1] };
                list.RemoveRange(0, 2);
                melds.Add(new Meld(MeldKind.Eyes, eyesTiles));
            }
        }

        return melds;
    }

    // Total score of all melds by evaluating each meld (using individual tile values).
    public int CalcAllMeldsScore(List<Meld> melds)
    {
        if (melds == null) return 0;
        int total = 0;
        foreach (var m in melds) total += EvalMeld(m);
        return total;
    }

    // Pung: X * Y. Chow: X * (Y - 1) with Y = highest in run. Eyes: (tile1 + tile2) * 2.
    public int EvalMeld(Meld meld)
    {
        if (meld.Tiles == null || meld.Tiles.Count == 0) return 0;

        int x = meld.Tiles.Count;
        if (meld.Kind == MeldKind.Pung)
        {
            int y = GetTileScore(meld.Tiles[0]);
            return x * y;
        }
        if (meld.Kind == MeldKind.Eyes)
        {
            int a = GetTileScore(meld.Tiles[0]);
            int b = GetTileScore(meld.Tiles[1]);
            return (a + b) * 2;
        }
        // Chow: Y = highest value in the run
        int yChow = 0;
        foreach (var t in meld.Tiles)
        {
            int s = GetTileScore(t);
            if (s > yChow) yChow = s;
        }
        return x * (yChow - 1);
    }
    
    // Grabs the key for the honor tile.
    // Currently only wind and dragon.
    private string GetHonorKey(MahjongTile tile)
    {
        switch (tile.TileType)
        {
            case TileType.Wind: return $"Wind_{tile.WindValue}";
            case TileType.Dragon: return $"Dragon_{tile.DragonValue}";
            default: return tile.TileType.ToString();
        }
    }

    // Checks if the tile is a bonus tile (currently only flower or season).
    private static bool IsBonusTile(MahjongTile tile)
    {
        return tile.TileType == TileType.Flower || tile.TileType == TileType.Season; // TODO: Add more bonus tiles here.
    }

    // Checks if the tile is in any meld.
    private static bool IsTileInAnyMeld(MahjongTile tile, List<Meld> melds)
    {
        if (melds == null) return false;
        foreach (var m in melds)
            if (m.Tiles != null && m.Tiles.Contains(tile)) return true;
        return false;
    }
    #endregion
}
