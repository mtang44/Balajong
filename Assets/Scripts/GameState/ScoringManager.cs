/* ScoringManager is a singleton class that will handle the scoring of the game.
Functions include: 
- CalcHandScore: Calculates the total score for a hand of tiles using the current meld formulas.
- HandMeetsThreshold: Determines if the hand meets or exceeds the checkpoint threshold.
- CalcDamage: Calculates the amount by which the hand falls short of the threshold.
- CalcReward: Calculates the reward for a hand that beats the threshold.

Example usage:
After discards or Charleston, pass the current hand tiles to the ScoringManager to get the score, damage, and reward:
int score = ScoringManager.Instance.CalcHandScore(currentHandTiles);
bool passed = ScoringManager.Instance.HandMeetsThreshold(currentHandTiles, checkpointThreshold);
int damage = ScoringManager.Instance.CalcDamage(currentHandTiles, checkpointThreshold);
int reward = ScoringManager.Instance.CalcReward(currentHandTiles, checkpointThreshold);
*/

using System.Collections.Generic;
using UnityEngine;

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance;

    [SerializeField]
    private bool logScoringDetails = false;

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

    #region Scoring Formulas
    // Pung: score = X * Y
    // Chow: score = X * (Y - 1)
    // X = meld size, Y = tile base value
    private static int CalcPungScore(int setSize, int tileValue) => setSize * tileValue;
    private static int CalcChowScore(int setSize, int highestValue) => setSize * (highestValue - 1);
    #endregion

    // Calculates the total score for a hand of tiles using the current meld formulas.
    // Pungs: Meld score = X * Y
    // Chows: Meld score = X * (Y - 1)
    // Where X is the size of the meld and Y is the tile value as defined in the deck comments.
    public int CalcHandScore(IReadOnlyList<MahjongTile> tiles)
    {
        if (tiles == null || tiles.Count == 0)
        {
            return 0;
        }

        // Suited tiles (Dots, Bam, Crack) by numeric value 1–9.
        var suitedCounts = new Dictionary<TileType, int[]>
        {
            { TileType.Dots, new int[10] },
            { TileType.Bam, new int[10] },
            { TileType.Crack, new int[10] }
        };

        // Honor tiles (winds, dragons) for pung detection.
        var honorCounts = new Dictionary<string, TileCount>();

        // Flowers and seasons are treated as simple bonuses.
        int flowerSeasonBonus = 0;

        foreach (var tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            switch (tile.TileType)
            {
                case TileType.Dots:
                case TileType.Bam:
                case TileType.Crack:
                {
                    int value = (int)tile.NumberedValue;
                    if (value >= 1 && value <= 9)
                    {
                        suitedCounts[tile.TileType][value]++;
                    }
                    break;
                }
                case TileType.Flower:
                case TileType.Season:
                {
                    flowerSeasonBonus += GetTileBaseValue(tile);
                    break;
                }
                case TileType.Wind:
                case TileType.Dragon:
                {
                    string key = GetHonorKey(tile);
                    if (!honorCounts.TryGetValue(key, out var info))
                    {
                        info = new TileCount
                        {
                            Count = 0,
                            BaseValue = GetTileBaseValue(tile)
                        };
                        honorCounts[key] = info;
                    }

                    info.Count++;
                    break;
                }
            }
        }

        int chowScore = ScoreChows(suitedCounts);
        int pungScore = ScorePungs(suitedCounts, honorCounts);
        int total = chowScore + pungScore + flowerSeasonBonus;

        if (logScoringDetails)
        {
            Debug.Log($"Hand score: total={total}, chows={chowScore}, pungs={pungScore}, flower/season bonus={flowerSeasonBonus}");
        }

        return total;
    }

    // Returns true if the hand meets or exceeds the checkpoint threshold.
    public bool HandMeetsThreshold(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        return CalcHandScore(tiles) >= checkpointThreshold;
    }

    // Damage is the amount by which the hand falls short of the threshold.
    // Returns 0 if the hand meets or exceeds the threshold.
    public int CalcDamage(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        int score = CalcHandScore(tiles);
        return score >= checkpointThreshold ? 0 : checkpointThreshold - score;
    }

    // Reward is how far above the threshold the hand scored.
    // Returns 0 if the hand does not beat the threshold.
    public int CalcReward(IReadOnlyList<MahjongTile> tiles, int checkpointThreshold)
    {
        int score = CalcHandScore(tiles);
        return score > checkpointThreshold ? score - checkpointThreshold : 0;
    }

    // Scores all chows (sequences of three suited tiles in the same suit).
    // For each chow of size X ending at value Y, score = X * (Y - 1).
    // CURRENTLY ONLY HANDLES SEQUENCES OF LENGTH 3.
    private int ScoreChows(Dictionary<TileType, int[]> suitedCounts)
    {
        int total = 0;

        foreach (var kvp in suitedCounts)
        {
            int[] counts = kvp.Value;
            bool found;

            // Extract chows from lowest value to highest value.
            do
            {
                found = false;

                for (int v = 1; v <= 7; v++)
                {
                    if (counts[v] > 0 && counts[v + 1] > 0 && counts[v + 2] > 0)
                    {
                        counts[v]--;
                        counts[v + 1]--;
                        counts[v + 2]--;

                        int X = 3;
                        int Y = v + 2; // highest tile in the chow
                        total += CalcChowScore(X, Y);

                        found = true;
                        break;
                    }
                }

            } while (found);
        }

        return total;
    }

    // Scores all pungs (sets of identical tiles).
    // For each set of size X with base value Y, score = X * Y.
    private int ScorePungs(Dictionary<TileType, int[]> suitedCounts, Dictionary<string, TileCount> honorCounts)
    {
        int total = 0;

        // Pungs in suited tiles after chows have been removed.
        // kvp is a key-value pair -> key is the tile type and the value is an array of counts for each numbered value.
        foreach (var kvp in suitedCounts)
        {
            int[] counts = kvp.Value;

            for (int v = 1; v <= 9; v++)
            {
                int count = counts[v];
                if (count < 3)
                {
                    continue;
                }

                int sets = count / 3;
                int X = 3;
                int Y = v;

                total += sets * CalcPungScore(X, Y);
                counts[v] -= sets * X;
            }
        }

        // Pungs in honor tiles (winds, dragons).
        foreach (var kvp in honorCounts)
        {
            TileCount info = kvp.Value;
            if (info.Count < 3)
            {
                continue;
            }

            int sets = info.Count / 3;
            int XHonor = 3;
            int YHonor = info.BaseValue;

            total += sets * CalcPungScore(XHonor, YHonor);
            info.Count -= sets * XHonor;
        }

        return total;
    }

    // Access and return the numeric "Y" value for a tile:
    // - 1–9 for numbered suits
    // - 1–4 for N/E/S/W
    // - 1–3 for R/G/W dragons
    // - 1–4 for Flowers/Seasons
    private int GetTileBaseValue(MahjongTile tile)
    {
        switch (tile.TileType)
        {
            case TileType.Dots:
            case TileType.Bam:
            case TileType.Crack:
                return (int)tile.NumberedValue;

            case TileType.Wind:
                switch (tile.WindValue)
                {
                    case WindValue.North: return 1;
                    case WindValue.East: return 2;
                    case WindValue.South: return 3;
                    case WindValue.West: return 4;
                    default: return 0;
                }

            case TileType.Dragon:
                switch (tile.DragonValue)
                {
                    case DragonValue.Red: return 1;
                    case DragonValue.Green: return 2;
                    case DragonValue.White: return 3;
                    default: return 0;
                }

            case TileType.Flower:
                switch (tile.FlowerValue)
                {
                    case FlowerValue.Plum: return 1;
                    case FlowerValue.Orchid: return 2;
                    case FlowerValue.Bamboo: return 3;
                    case FlowerValue.Chrysanthemum: return 4;
                    default: return 0;
                }

            case TileType.Season:
                switch (tile.SeasonValue)
                {
                    case SeasonValue.Spring: return 1;
                    case SeasonValue.Summer: return 2;
                    case SeasonValue.Autumn: return 3;
                    case SeasonValue.Winter: return 4;
                    default: return 0;
                }

            default:
                return 0;
        }
    }

    private string GetHonorKey(MahjongTile tile)
    {
        switch (tile.TileType)
        {
            case TileType.Wind:
                return $"Wind_{tile.WindValue}";
            case TileType.Dragon:
                return $"Dragon_{tile.DragonValue}";
            default:
                return tile.TileType.ToString();
        }
    }

    private class TileCount
    {
        public int Count;
        public int BaseValue;
    }
}