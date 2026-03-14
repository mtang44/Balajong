/* ScoringManager is a singleton class that will handle the scoring of the game. 
- Scores implictly by building its own buckets by suit + value then finds all melds.

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
using System.Text;
using UnityEngine;

// Alias so callers can pass MahjongTileData lists.
using MahjongTile = MahjongTileData;

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance;

    [SerializeField]
    private bool logScoringDetails = false;

    [SerializeField]
    private GameObject handInfoTextObject;

    [SerializeField]
    private bool autoUpdateHandInfo = true;

    [SerializeField]
    private bool showTotal = false;

    private TMPro.TextMeshProUGUI handInfoTmpText;
    private UnityEngine.UI.Text handInfoLegacyText;
    private string lastRenderedHandInfo = string.Empty;

    #region Tile values by type (all 0; set from a separate file that defines scores per TileType)

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

    private void Start()
    {
        UpdateHandInfoDisplay();
    }

    private void Update()
    {
        if (autoUpdateHandInfo)
            UpdateHandInfoDisplay();
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
        //commenting this out, as I believe calling the flowersinhandandseasonsinhand are faster
        // foreach (var tile in tiles)
        // {
        //     if (tile == null) continue;
        //     if (IsBonusTile(tile) && !IsTileInAnyMeld(tile, melds))
        //         bonusScore += GetTileScore(tile);
        // }
        // Add bonus score for any flower or season tiles not in melds
        foreach (var tile in DeckManager.Instance.flowerTiles)
        {
            if (tile == null) continue;
            var tileData = tile.GetComponent<MahjongTileHolder>()?.TileData;
            if (tileData != null)
                bonusScore += GetTileScore(tileData);
        }
        foreach (var tile in DeckManager.Instance.seasonTiles)
        {
            if (tile == null) continue;
            var tileData = tile.GetComponent<MahjongTileHolder>()?.TileData;
            if (tileData != null)
                bonusScore += GetTileScore(tileData);
        }

        int total = meldScore + bonusScore;
        if (logScoringDetails)
            Debug.Log($"Hand score: total={total}, melds={meldScore}, bonus={bonusScore}");
        return total;
    }

    // Updates the optional hand-info UI text with the current scoring breakdown.
    public void UpdateHandInfoDisplay()
    {
        if (DeckManager.Instance == null) return;
        if (!TryResolveHandInfoText()) return;

        string handInfo = BuildCurrentHandInfoText();
        if (handInfo == lastRenderedHandInfo) return;

        if (handInfoTmpText != null)
            handInfoTmpText.text = handInfo;
        else if (handInfoLegacyText != null)
            handInfoLegacyText.text = handInfo;

        lastRenderedHandInfo = handInfo;
    }

    private bool TryResolveHandInfoText()
    {
        if (handInfoTmpText != null || handInfoLegacyText != null)
            return true;

        if (handInfoTextObject == null)
            return false;

        handInfoTmpText = handInfoTextObject.GetComponent<TMPro.TextMeshProUGUI>();
        if (handInfoTmpText == null)
            handInfoLegacyText = handInfoTextObject.GetComponent<UnityEngine.UI.Text>();

        return handInfoTmpText != null || handInfoLegacyText != null;
    }

    private string BuildCurrentHandInfoText()
    {
        List<MahjongTile> handTiles = DeckManager.Instance.getHandAsMahjongTileData();
        List<Meld> melds = DetectMelds(handTiles);

        int eyesCount = 0;
        int pungCount = 0;
        int kongCount = 0;
        int quintCount = 0;
        int balajongCount = 0;
        int chowCount = 0;

        int meldTotal = 0;
        foreach (var meld in melds)
        {
            int meldScore = EvalMeld(meld);
            meldTotal += meldScore;

            switch (meld.Kind)
            {
                case MeldKind.Eyes:
                    eyesCount++;
                    break;
                case MeldKind.Pung:
                    pungCount++;
                    break;
                case MeldKind.Kong:
                    kongCount++;
                    break;
                case MeldKind.Quint:
                    quintCount++;
                    break;
                case MeldKind.Balajong:
                    balajongCount++;
                    break;
                case MeldKind.Chow:
                    chowCount++;
                    break;
            }
        }

        int flowerTotal = AddBonusBreakdown(DeckManager.Instance.flowerTiles, out int flowerCount);
        int seasonTotal = AddBonusBreakdown(DeckManager.Instance.seasonTiles, out int seasonCount);

        int total = meldTotal + flowerTotal + seasonTotal;

        var sb = new StringBuilder();
        sb.AppendLine("Hand Info:");
        AppendTypeCountLine(sb, "Eyes", eyesCount);
        AppendTypeCountLine(sb, "Pung", pungCount);
        AppendTypeCountLine(sb, "Kong", kongCount);
        AppendTypeCountLine(sb, "Quint", quintCount);
        AppendTypeCountLine(sb, "BALAJONG", balajongCount);
        AppendTypeCountLine(sb, "Chow", chowCount);
        AppendTypeCountLine(sb, "Flowers", flowerCount);
        AppendTypeCountLine(sb, "Seasons", seasonCount);
        if (showTotal)
            sb.Append($"Total - {total}");

        return sb.ToString().TrimEnd();
    }

    private int AddBonusBreakdown(List<GameObject> tiles, out int tileCount)
    {
        tileCount = 0;
        if (tiles == null) return 0;

        int total = 0;
        foreach (var tile in tiles)
        {
            if (tile == null) continue;

            MahjongTile tileData = tile.GetComponent<MahjongTileHolder>()?.TileData;
            if (tileData == null) continue;

            int tileScore = GetTileScore(tileData);
            total += tileScore;
            tileCount++;
        }

        return total;
    }

    private static void AppendTypeCountLine(StringBuilder sb, string label, int count)
    {
        if (count <= 0)
            return;

        sb.AppendLine($"{label} x{count}");
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

    public enum MeldKind { Chow, Pung, Kong, Quint, Balajong, Eyes }

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

        // Chow: 3 consecutive tiles in same suit (1-2-3, 2-3-4, ... 7-8-9). Extract first so tiles aren't double-counted.
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

        // Detect from largest to smallest: Balajong (6+), then Quint (5), Kong (4), Pung (3), Eyes (2). No overlap.
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 9; v++)
            {
                var list = suited[suit][v];
                while (list.Count >= 6)
                {
                    int n = list.Count;
                    var balajongTiles = new List<MahjongTile>();
                    for (int i = 0; i < n; i++)
                        balajongTiles.Add(list[i]);
                    list.RemoveRange(0, n);
                    melds.Add(new Meld(MeldKind.Balajong, balajongTiles));
                }
                while (list.Count >= 5)
                {
                    var quintTiles = new List<MahjongTile> { list[0], list[1], list[2], list[3], list[4] };
                    list.RemoveRange(0, 5);
                    melds.Add(new Meld(MeldKind.Quint, quintTiles));
                }
                while (list.Count >= 4)
                {
                    var kongTiles = new List<MahjongTile> { list[0], list[1], list[2], list[3] };
                    list.RemoveRange(0, 4);
                    melds.Add(new Meld(MeldKind.Kong, kongTiles));
                }
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
            while (list.Count >= 6)
            {
                int n = list.Count;
                var balajongTiles = new List<MahjongTile>();
                for (int i = 0; i < n; i++)
                    balajongTiles.Add(list[i]);
                list.RemoveRange(0, n);
                melds.Add(new Meld(MeldKind.Balajong, balajongTiles));
            }
            while (list.Count >= 5)
            {
                var quintTiles = new List<MahjongTile> { list[0], list[1], list[2], list[3], list[4] };
                list.RemoveRange(0, 5);
                melds.Add(new Meld(MeldKind.Quint, quintTiles));
            }
            while (list.Count >= 4)
            {
                var kongTiles = new List<MahjongTile> { list[0], list[1], list[2], list[3] };
                list.RemoveRange(0, 4);
                melds.Add(new Meld(MeldKind.Kong, kongTiles));
            }
            while (list.Count >= 3)
            {
                var pungTiles = new List<MahjongTile> { list[0], list[1], list[2] };
                list.RemoveRange(0, 3);
                melds.Add(new Meld(MeldKind.Pung, pungTiles));
            }
        }

        // Extract eyes (pairs): two identical tiles; only what's left after kongs/pungs
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

    // Eyes: (tile1 + tile2) * 2. Pung: (t1+t2+t3)*3. Kong: (t1..t4)*4. Quint: (t1..t5)*5. Balajong: (t1+..+tn)*n (n>=6). Chow: sum of 3 tiles + 20.
    public int EvalMeld(Meld meld)
    {
        if (meld.Tiles == null || meld.Tiles.Count == 0) return 0;

        if (meld.Kind == MeldKind.Eyes && meld.Tiles.Count == 2)
        {
            int a = GetTileScore(meld.Tiles[0]);
            int b = GetTileScore(meld.Tiles[1]);
            return (a + b) * 2;
        }
        if (meld.Kind == MeldKind.Pung && meld.Tiles.Count == 3)
        {
            int sum = GetTileScore(meld.Tiles[0]) + GetTileScore(meld.Tiles[1]) + GetTileScore(meld.Tiles[2]);
            return sum * 3;
        }
        if (meld.Kind == MeldKind.Kong && meld.Tiles.Count == 4)
        {
            int sum = GetTileScore(meld.Tiles[0]) + GetTileScore(meld.Tiles[1]) + GetTileScore(meld.Tiles[2]) + GetTileScore(meld.Tiles[3]);
            return sum * 4;
        }
        if (meld.Kind == MeldKind.Quint && meld.Tiles.Count == 5)
        {
            int sum = GetTileScore(meld.Tiles[0]) + GetTileScore(meld.Tiles[1]) + GetTileScore(meld.Tiles[2]) + GetTileScore(meld.Tiles[3]) + GetTileScore(meld.Tiles[4]);
            return sum * 5;
        }
        if (meld.Kind == MeldKind.Balajong && meld.Tiles.Count >= 6)
        {
            int sum = 0;
            foreach (var t in meld.Tiles)
                sum += GetTileScore(t);
            return sum * meld.Tiles.Count;
        }
        if (meld.Kind == MeldKind.Chow)
        {
            int sum = 0;
            foreach (var t in meld.Tiles)
                sum += GetTileScore(t);
            return sum + 20;
        }
        return 0;
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
