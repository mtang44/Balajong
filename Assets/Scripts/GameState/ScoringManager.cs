/* ScoringManager is a singleton class that will handle the scoring of the game. 
- Scores implictly by building its own buckets by suit + value then finds all melds.

Main functions:
- GetTileScore(tile): Returns the configured score value for a single tile (by type).
- CalcHandScore(tiles): Total score for a hand using tile values and meld formulas.
- HandMeetsThreshold(tiles, checkpointThreshold): True if hand meets or exceeds threshold.
- CalcDamage(tiles, checkpointThreshold): Amount hand falls short of threshold (0 if passed).
- CalcReward(tiles, checkpointThreshold): Amount hand beats threshold by (0 if not).
- DetectMelds(tiles): Returns all detected melds (single/sequence/set/honor patterns) as Meld objects.
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

    bool winded = false;

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
    public int GetTileScore(MahjongTile tile, bool modifyJokers = false) //CHARLES WILL ADD ADDITIONAL JOKER CHECKS
    {
        if (tile == null) return 0;
        int acc = 0;
        switch (tile.TileType)
        {
            case TileType.Dots:
                {
                    for(int i = 0; i < JokerManager.Instance.numberOfActivations("blue"); i++)
                    {
                        acc += 10;
                    }
                    acc += GetSuitedValue(dotsValues, (int)tile.NumberedValue);
                    break;
                }
            case TileType.Bam:
                {
                    for(int i = 0; i < JokerManager.Instance.numberOfActivations("green"); i++)
                    {
                        acc += 10;
                    }
                    acc += GetSuitedValue(bamValues, (int)tile.NumberedValue);
                    break;
                }
            case TileType.Crack:
                {
                    for(int i = 0; i < JokerManager.Instance.numberOfActivations("red"); i++)
                    {
                        acc += 10;
                    }
                    acc += GetSuitedValue(crackValues, (int)tile.NumberedValue);
                    break;
                }
            case TileType.Wind:
                {
                    winded = true;
                    if(JokerManager.Instance.jokers.Contains("bagged") && modifyJokers)
                        JokerManager.Instance.baggedJokerBuff++;
                    acc += GetWindValue(tile.WindValue);
                    break;
                }
            case TileType.Dragon:
                {
                    acc += GetDragonValue(tile.DragonValue);
                    break;
                }
            case TileType.Flower:
                {
                    acc += GetFlowerValue(tile.FlowerValue);
                    break;
                }
            case TileType.Season:
                {
                    acc += GetSeasonValue(tile.SeasonValue);
                    break;
                }
            default:
                return 0;
        }
        if(tile.Edition == Edition.Ghost)
            {
                if (JokerManager.Instance.numberOfActivations("grave") > 0)
                    acc += 100;
                else
                    acc += 50;
            }
        return acc;
    }

    // Returns base tile face value only (no joker or edition modifiers).
    public int GetTileFaceValue(MahjongTile tile)
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

        winded = false;
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

        int jokerBonusPoints = JokerPoints();
        int jokerMultiplier = jokerMult();
        int jokerMultMultiplier = jokerMultMult();
        int newTotal = ((total + jokerBonusPoints) * jokerMultiplier) * jokerMultMultiplier;
        if (logScoringDetails)
            Debug.Log($"Rack score: total={newTotal}, melds={meldScore}, bonus={bonusScore}, jokerBonus={jokerBonusPoints}, jokerMult={jokerMultiplier}" + (jokerMultMultiplier > 1 ? $", jokerMultMult={jokerMultMultiplier}" : ""));
        return newTotal;
    }

    //THESE FUNCTIONS TO GET THE POINTS FROM OVERALL BONUS JOKERS
    int JokerPoints()
    {
        int acc = 0;
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("bagged"); i++) //IMPLEMENT THE DISCARD BONUS
        {
            acc += JokerManager.Instance.baggedJokerBuff * 30;
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("fishdish"); i++)
        {
            acc += Random.Range(10,50);
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("ledger"); i++)
        {
            acc += Random.Range(20,80);
        }
        return acc;
    }
    int jokerMult()
    {
        int acc = 1;
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("joker"); i++)
        {
            acc += 3;
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("hatcat"); i++)
        {
            acc += Random.Range(2,10);
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("ledger"); i++)
        {
            acc += Random.Range(5,15);
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("knight"); i++) //IMPLEMENT THE DISCARD BONUS
        {
            acc += JokerManager.Instance.knightJokerBuff;
        }
        return acc;
    }
    int jokerMultMult()
    {
        int acc = 1;
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("spider"); i++)
        {
            acc *= (PlayerStatManager.Instance.maxHealth - PlayerStatManager.Instance.currentHealth) + 1;
        }
        return acc;
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

        int singleCount = 0;
        int eyesCount = 0;
        int pungCount = 0;
        int kongCount = 0;
        int quintCount = 0;
        int balajongCount = 0;
        int newsCount = 0;
        int hydraCount = 0;
        int chowCount = 0;
        int jogCount = 0;
        int sprintCount = 0;

        int meldTotal = 0;
        foreach (var meld in melds)
        {
            int meldScore = EvalMeld(meld);
            meldTotal += meldScore;

            switch (meld.Kind)
            {
                case MeldKind.Single:
                    singleCount++;
                    break;
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
                case MeldKind.News:
                    newsCount++;
                    break;
                case MeldKind.Hydra:
                    hydraCount++;
                    break;
                case MeldKind.Chow:
                    chowCount++;
                    break;
                case MeldKind.Jog:
                    jogCount++;
                    break;
                case MeldKind.Sprint:
                    sprintCount++;
                    break;
            }
        }

        int flowerTotal = AddBonusBreakdown(DeckManager.Instance.flowerTiles, out int flowerCount);
        int seasonTotal = AddBonusBreakdown(DeckManager.Instance.seasonTiles, out int seasonCount);

        int total = meldTotal + flowerTotal + seasonTotal;

        var sb = new StringBuilder();
        sb.AppendLine("Hand Info:");
        AppendTypeCountLine(sb, "Single", singleCount);     // Single tile (not involved in any meld)
        AppendTypeCountLine(sb, "Eyes", eyesCount);         // Pair
        AppendTypeCountLine(sb, "Pung", pungCount);         // Three of a kind
        AppendTypeCountLine(sb, "Kong", kongCount);         // Four of a kind
        AppendTypeCountLine(sb, "Quint", quintCount);       // Five of a kind
        AppendTypeCountLine(sb, "BALAJONG", balajongCount); // Six of more of a kind
        AppendTypeCountLine(sb, "NEWS", newsCount);         // One of each wind: N E W S
        AppendTypeCountLine(sb, "Hydra", hydraCount);       // One each dragon: red, green, white
        AppendTypeCountLine(sb, "Chow", chowCount);         // Sequence of three consecutive tiles
        AppendTypeCountLine(sb, "Jog", jogCount);           // Sequence of four consecutive suited tiles
        AppendTypeCountLine(sb, "Sprint", sprintCount);     // Sequence of five consecutive suited tiles
        AppendTypeCountLine(sb, "Flowers", flowerCount);    // Flower bonus tiles
        AppendTypeCountLine(sb, "Seasons", seasonCount);    // Season bonus tiles
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

    public enum MeldKind { Single, Eyes, Chow, Jog, Sprint, Pung, Kong, Quint, Balajong, Hydra, News }

    // Meld struct for storing the kind and tiles of a meld.
    public struct Meld
    {
        public MeldKind Kind;
        public List<MahjongTile> Tiles; // size depends on kind (single, sequence, set, etc.)

        public Meld(MeldKind kind, List<MahjongTile> tiles)
        {
            Kind = kind;
            Tiles = tiles ?? new List<MahjongTile>();
        }
    }

    // Detects all melds in the hand. Returns meld objects that reference the actual tiles.
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
        List<MahjongTile> northWinds = GetOrCreateHonorList(honorLists, $"Wind_{WindValue.North}");
        List<MahjongTile> eastWinds = GetOrCreateHonorList(honorLists, $"Wind_{WindValue.East}");
        List<MahjongTile> southWinds = GetOrCreateHonorList(honorLists, $"Wind_{WindValue.South}");
        List<MahjongTile> westWinds = GetOrCreateHonorList(honorLists, $"Wind_{WindValue.West}");
        List<MahjongTile> redDragons = GetOrCreateHonorList(honorLists, $"Dragon_{DragonValue.Red}");
        List<MahjongTile> greenDragons = GetOrCreateHonorList(honorLists, $"Dragon_{DragonValue.Green}");
        List<MahjongTile> whiteDragons = GetOrCreateHonorList(honorLists, $"Dragon_{DragonValue.White}");

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
                    GetOrCreateHonorList(honorLists, key).Add(tile);
                    break;
            }
        }

        // Sprint: 5 consecutive tiles in same suit (1-2-3-4-5 .. 5-6-7-8-9).
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 5; v++)
            {
                var t1 = suited[suit][v];
                var t2 = suited[suit][v + 1];
                var t3 = suited[suit][v + 2];
                var t4 = suited[suit][v + 3];
                var t5 = suited[suit][v + 4];
                while (t1.Count > 0 && t2.Count > 0 && t3.Count > 0 && t4.Count > 0 && t5.Count > 0)
                {
                    var sprintTiles = new List<MahjongTile> { t1[0], t2[0], t3[0], t4[0], t5[0] };
                    t1.RemoveAt(0); t2.RemoveAt(0); t3.RemoveAt(0); t4.RemoveAt(0); t5.RemoveAt(0);
                    melds.Add(new Meld(MeldKind.Sprint, sprintTiles));
                }
            }
        }

        // Jog: 4 consecutive tiles in same suit (1-2-3-4 .. 6-7-8-9).
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 6; v++)
            {
                var t1 = suited[suit][v];
                var t2 = suited[suit][v + 1];
                var t3 = suited[suit][v + 2];
                var t4 = suited[suit][v + 3];
                while (t1.Count > 0 && t2.Count > 0 && t3.Count > 0 && t4.Count > 0)
                {
                    var jogTiles = new List<MahjongTile> { t1[0], t2[0], t3[0], t4[0] };
                    t1.RemoveAt(0); t2.RemoveAt(0); t3.RemoveAt(0); t4.RemoveAt(0);
                    melds.Add(new Meld(MeldKind.Jog, jogTiles));
                }
            }
        }

        // Chow: 3 consecutive tiles in same suit (1-2-3, 2-3-4, ... 7-8-9).
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

        // Same-value suited melds from largest to smallest: Balajong (6+), then Quint (5), Kong (4), Pung (3).
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

        // NEWS: one of each wind (North, East, West, South).
        while (northWinds.Count > 0 && eastWinds.Count > 0 && westWinds.Count > 0 && southWinds.Count > 0)
        {
            var newsTiles = new List<MahjongTile> { northWinds[0], eastWinds[0], westWinds[0], southWinds[0] };
            northWinds.RemoveAt(0); eastWinds.RemoveAt(0); westWinds.RemoveAt(0); southWinds.RemoveAt(0);
            melds.Add(new Meld(MeldKind.News, newsTiles));
        }

        // Hydra: one of each dragon (Red, Green, White).
        while (redDragons.Count > 0 && greenDragons.Count > 0 && whiteDragons.Count > 0)
        {
            var hydraTiles = new List<MahjongTile> { redDragons[0], greenDragons[0], whiteDragons[0] };
            redDragons.RemoveAt(0); greenDragons.RemoveAt(0); whiteDragons.RemoveAt(0);
            melds.Add(new Meld(MeldKind.Hydra, hydraTiles));
        }

        // Same-value honor melds from largest to smallest: Balajong (6+), then Quint (5), Kong (4), Pung (3).
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

        // Eyes (pairs): two identical tiles from leftovers.
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

        // Singles: any remaining tile not included in another meld.
        foreach (var suit in new[] { TileType.Dots, TileType.Bam, TileType.Crack })
        {
            for (int v = 1; v <= 9; v++)
            {
                var list = suited[suit][v];
                while (list.Count > 0)
                {
                    var singleTile = new List<MahjongTile> { list[0] };
                    list.RemoveAt(0);
                    melds.Add(new Meld(MeldKind.Single, singleTile));
                }
            }
        }

        foreach (var kvp in honorLists)
        {
            var list = kvp.Value;
            while (list.Count > 0)
            {
                var singleTile = new List<MahjongTile> { list[0] };
                list.RemoveAt(0);
                melds.Add(new Meld(MeldKind.Single, singleTile));
            }
        }

        return melds;
    }

    // Total score of all melds by evaluating each meld (using individual tile values).
    public int CalcAllMeldsScore(List<Meld> melds, bool finalCalc = false)
    {
        if (melds == null) return 0;
        int total = 0;
        foreach (var m in melds) total += EvalMeld(m, true);
        return total;
    }

    // Gets the multiplier points from any joker activations that apply to melds.
    float jokerMeldMultPoints(float og, Meld meld)
    {
        float acc = og;
        bool dot = false;
        bool bam = false;
        bool crack = false;
        foreach(var tile in meld.Tiles)
        {
            if(tile.Edition == Edition.Crystal)
            {
                if (JokerManager.Instance.numberOfActivations("rainbow") > 0)
                    acc += 10;
                else
                    acc += 5;
            }
            if(tile.TileType == TileType.Dots)
                dot = true;
            if(tile.TileType == TileType.Bam)
                bam = true;
            if(tile.TileType == TileType.Crack)
                crack = true;
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("polar"); i++)
        {
            if(dot) acc += 5;
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("grizzly"); i++)
        {
            if(crack) acc += 5;
        }
        for(int i = 0; i < JokerManager.Instance.numberOfActivations("panda"); i++)
        {
            if(bam) acc += 5;
        }
        return acc;
    }
    // Gets the multiplier multiplier points from any joker activations that apply to melds.
    float jokerMeldMultMaxxingPoints(float og, Meld meld)
    {
        float acc = og;
        foreach(var tile in meld.Tiles)
        {
            if(tile.Edition == Edition.Enchanted)
            {
                if (JokerManager.Instance.numberOfActivations("unbreaking") > 0)
                    acc *= 2.0f;
                else
                    acc *= 1.5f;
            }
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("ancalagon"); i++)
            {
                if(tile.TileType == TileType.Dragon && winded)
                {
                    acc *= 2.0f;
                }
            }
        }
        return acc;
    }

    // Single: tile face value. Eyes: (tile1 + tile2) * 2. Pung: (t1+t2+t3)*3.
    // Kong: (t1..t4)*4. Quint: (t1..t5)*5. Balajong: (t1+..+tn)*n (n>=6).
    // Chow: sum of 3 tiles * 3. Jog: ceil(sum of 4 tiles * 3.5).
    // Sprint: sum of 5 tiles * 4. Hydra: sum of 3 dragons * 3. NEWS: sum of 4 winds * 4.
    // BALAJONG: sum of 6 or more tiles * number of tiles.
    public int EvalMeld(Meld meld, bool finalCalc = false)
    {
        if (meld.Tiles == null || meld.Tiles.Count == 0) return 0;

        int sum = 0;
        float multTotal = 1;
        float multMultTotal = 1;
        bool meldContainsWind = false;
        foreach (var t in meld.Tiles)
        {
            sum += GetTileScore(t, finalCalc);
            if (t.TileType == TileType.Wind)
                meldContainsWind = true;
            multTotal = jokerMeldMultPoints(multTotal, meld);
            multMultTotal = jokerMeldMultMaxxingPoints(multMultTotal, meld);
        }

        if (meldContainsWind)
        {
            for (int i = 0; i < JokerManager.Instance.numberOfActivations("secondwind"); i++)
            {
                multMultTotal *= 2.0f;
            }
        }

        if (meld.Kind == MeldKind.Single && meld.Tiles.Count == 1)
            return sum;

        if (meld.Kind == MeldKind.Eyes && meld.Tiles.Count == 2)
        {
            multTotal += 1;
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("eyes"); i++)
            {
                multMultTotal *= 2.0f;
            }
        }
        if (meld.Kind == MeldKind.Pung && meld.Tiles.Count == 3)
        {
            multTotal += 2;
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("three"); i++)
            {
                multMultTotal *= 3.0f;
            }
        }
        if (meld.Kind == MeldKind.Kong && meld.Tiles.Count == 4)
        {
            multTotal += 3;
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("clover"); i++)
            {
                multMultTotal *= 4.0f;
            }
        }
        if (meld.Kind == MeldKind.Quint && meld.Tiles.Count == 5)
        {
            multTotal += 4;
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("clover"); i++)
            {
                multMultTotal *= 4.0f;
            }
        }
        if (meld.Kind == MeldKind.Balajong && meld.Tiles.Count >= 6)
        {
            multTotal += meld.Tiles.Count - 1;
            for(int i = 0; i < JokerManager.Instance.numberOfActivations("clover"); i++)
            {
                multMultTotal *= 4.0f;
            }
        }
        if (meld.Kind == MeldKind.Hydra && meld.Tiles.Count == 3)
        {
            multTotal += 2;
        }
        if (meld.Kind == MeldKind.News && meld.Tiles.Count == 4)
        {
            multTotal += 3;
        }
        if (meld.Kind == MeldKind.Chow && meld.Tiles.Count == 3)
        {
            multTotal += 3;
        }
        if (meld.Kind == MeldKind.Jog && meld.Tiles.Count == 4)
        {
            multTotal += 3.5f;
        }
        if (meld.Kind == MeldKind.Sprint && meld.Tiles.Count == 5)
        {
            multTotal += 4;
        }
        return sum * (int)(multTotal * multMultTotal);
    }

    private static List<MahjongTile> GetOrCreateHonorList(Dictionary<string, List<MahjongTile>> honorLists, string key)
    {
        if (!honorLists.TryGetValue(key, out List<MahjongTile> list))
        {
            list = new List<MahjongTile>();
            honorLists[key] = list;
        }

        return list;
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
