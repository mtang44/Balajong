using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Drives the end-of-round score animation.
// The hand is read left to right; when the last tile of each meld is reached a
// "+score" popup rises from it and the score counter ticks up.
// Flower/season bonus tiles are shown afterwards.
// Attach to any persistent scene GameObject.
public class ScoreVisualization : MonoBehaviour
{
    public static ScoreVisualization Instance;

    [Header("Popup Style")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField, Min(0.1f)] private float fontSize = 5f;
    [SerializeField] private Color handTypeTextColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color scoreTextColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private Vector3 popupWorldOffset = new Vector3(0f, 0.9f, -0.5f);
    [SerializeField, Min(0f)] private float horizontalEdgePadding = 24f;
    [SerializeField, Min(0f)]   private float popupRiseDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float popupLifetime     = 1.2f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float delayPerTile    = 0.15f;
    [SerializeField, Min(0f)] private float pauseAfterMeld  = 0.35f;
    [SerializeField, Min(0f)] private float pauseAfterBonus = 0.25f;
    [SerializeField, Min(0f)] private float pauseBetweenSections = 0.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Animate the score readout starting from <paramref name="baseScore"/>.
    // Yield on this coroutine from GameManager; it resolves when the animation finishes.
    public IEnumerator AnimateScore(int baseScore)
    {
        DeckManager    deck    = DeckManager.Instance;
        ScoringManager scoring = ScoringManager.Instance;

        // Running display value, boxed so it can be shared with ShowBonusTiles
        int[] runningScore = { baseScore };

        List<GameObject> hand = deck.hand;

        if (hand == null || hand.Count == 0)
        {
            bool hasFlowers = HasScorableBonusTiles(deck.flowerTiles);
            bool hasSeasons = HasScorableBonusTiles(deck.seasonTiles);

            if (hasFlowers)
            {
                yield return ShowBonusTiles(deck.flowerTiles, runningScore);
            }

            if (hasSeasons)
            {
                if (hasFlowers && pauseBetweenSections > 0f)
                {
                    yield return new WaitForSeconds(pauseBetweenSections);
                }

                yield return ShowBonusTiles(deck.seasonTiles, runningScore);
            }

            yield break;
        }

        // getHandAsMahjongTileData returns the exact same MahjongTileData
        // instances that are stored in the holders, so reference equality
        // correctly links Meld.Tiles back to hand GameObjects.
        List<MahjongTileData>      handData  = deck.getHandAsMahjongTileData();
        List<ScoringManager.Meld>  melds     = scoring.DetectMelds(handData);

        Dictionary<MahjongTileData, int> tileIndex =
            new Dictionary<MahjongTileData, int>(hand.Count);

        for (int i = 0; i < hand.Count; i++)
        {
            MahjongTileData td = hand[i]?.GetComponent<MahjongTileHolder>()?.TileData;
            if (td != null && !tileIndex.ContainsKey(td))
                tileIndex[td] = i;
        }

        // slotScore[i] = -1  -> tile not in any meld (no popup, no pause)
        // slotScore[i] =  0  -> tile is in a meld but not the trigger slot
        // slotScore[i] >  0  -> rightmost tile of a meld; this score pops here
        int[] slotScore = new int[hand.Count];
        string[] slotTypeLabel = new string[hand.Count];
        for (int i = 0; i < hand.Count; i++) slotScore[i] = -1;

        foreach (ScoringManager.Meld meld in melds)
        {
            List<int> meldIndices = new List<int>(meld.Tiles.Count);
            foreach (MahjongTileData td in meld.Tiles)
            {
                if (tileIndex.TryGetValue(td, out int idx))
                    meldIndices.Add(idx);
            }
            if (meldIndices.Count == 0) continue;

            meldIndices.Sort();
            int thisMeldScore = scoring.EvalMeld(meld);

            foreach (int idx in meldIndices)
                slotScore[idx] = 0;

            int triggerIndex = meldIndices[meldIndices.Count - 1];
            slotScore[triggerIndex] = thisMeldScore;
            slotTypeLabel[triggerIndex] = GetMeldDisplayName(meld.Kind);
        }

        // Go along the hand left to right, showing popups for melds
        for (int i = 0; i < hand.Count; i++)
        {
            if (slotScore[i] < 0)
                continue; // not in any meld – no delay needed

            yield return new WaitForSeconds(delayPerTile);

            if (slotScore[i] > 0) // trigger tile: fire the popup
            {
                Vector3 spawnPos = hand[i].transform.position + popupWorldOffset;
                string handType = string.IsNullOrEmpty(slotTypeLabel[i]) ? "Meld" : slotTypeLabel[i];
                SpawnPopup(spawnPos, handType, slotScore[i], handTypeTextColor, scoreTextColor);

                runningScore[0] += slotScore[i];
                StatsUpdater.Instance.UpdateScore(runningScore[0]);

                yield return new WaitForSeconds(pauseAfterMeld);
            }
        }

        // Flower and season bonuses
        bool hasFlowerBonuses = HasScorableBonusTiles(deck.flowerTiles);
        bool hasSeasonBonuses = HasScorableBonusTiles(deck.seasonTiles);

        if (hasFlowerBonuses)
        {
            if (pauseBetweenSections > 0f)
            {
                yield return new WaitForSeconds(pauseBetweenSections);
            }

            yield return ShowBonusTiles(deck.flowerTiles, runningScore);
        }

        if (hasSeasonBonuses)
        {
            if (pauseBetweenSections > 0f)
            {
                yield return new WaitForSeconds(pauseBetweenSections);
            }

            yield return ShowBonusTiles(deck.seasonTiles, runningScore);
        }
    }

    private bool HasScorableBonusTiles(List<GameObject> tiles)
    {
        if (tiles == null || tiles.Count == 0)
        {
            return false;
        }

        foreach (GameObject tile in tiles)
        {
            if (tile == null) continue;

            MahjongTileData tileData = tile.GetComponent<MahjongTileHolder>()?.TileData;
            if (tileData == null) continue;

            if (ScoringManager.Instance.GetTileScore(tileData) > 0)
            {
                return true;
            }
        }

        return false;
    }

    // Helper to show bonus tiles (flowers/seasons)
    private IEnumerator ShowBonusTiles(List<GameObject> tiles, int[] runningScore)
    {
        if (tiles == null) yield break;

        foreach (GameObject tile in tiles)
        {
            if (tile == null) continue;

            MahjongTileData td = tile.GetComponent<MahjongTileHolder>()?.TileData;
            if (td == null) continue;

            int bonus = ScoringManager.Instance.GetTileScore(td);
            if (bonus <= 0) continue;

            Vector3 spawnPos = tile.transform.position + popupWorldOffset;
            string handType = GetBonusDisplayName(td);
            SpawnPopup(spawnPos, handType, bonus, handTypeTextColor, scoreTextColor);

            runningScore[0] += bonus;
            StatsUpdater.Instance.UpdateScore(runningScore[0]);

            yield return new WaitForSeconds(pauseAfterBonus);
        }
    }

    private void SpawnPopup(Vector3 worldPos, string handType, int scoreValue, Color handTypeColor, Color scoreColor)
    {
        GameObject go = new GameObject("ScorePopup");
        Camera cam = Camera.main;
        go.transform.position = ClampWorldPositionX(worldPos, cam);

        // Billboard: copy camera rotation so text always faces the player
        if (cam != null)
            go.transform.rotation = cam.transform.rotation;

        TextMeshPro tmp      = go.AddComponent<TextMeshPro>();
        tmp.text             = BuildPopupText(handType, scoreValue, handTypeColor, scoreColor);
        tmp.fontSize         = fontSize;
        tmp.color            = Color.white;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        if (fontAsset != null) tmp.font = fontAsset;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 100;

        StartCoroutine(AnimatePopup(go));
    }

    private IEnumerator AnimatePopup(GameObject go)
    {
        if (go == null) yield break;

        TextMeshPro tmp      = go.GetComponent<TextMeshPro>();
        Vector3     startPos = go.transform.position;
        float       elapsed  = 0f;

        while (elapsed < popupLifetime && go != null)
        {
            float t = elapsed / popupLifetime;

            Vector3 targetPos = startPos + Vector3.up * (popupRiseDistance * t);
            go.transform.position = ClampWorldPositionX(targetPos, Camera.main);

            // Full opacity for the first half, then ease out
            float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
            if (tmp != null)
            {
                tmp.alpha = alpha;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private Vector3 ClampWorldPositionX(Vector3 worldPos, Camera cam)
    {
        if (cam == null)
        {
            return worldPos;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f)
        {
            return worldPos;
        }

        float padding = Mathf.Max(0f, horizontalEdgePadding);
        float minX = padding;
        float maxX = Screen.width - padding;

        if (minX > maxX)
        {
            return worldPos;
        }

        screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
        return cam.ScreenToWorldPoint(screenPos);
    }

    private static string GetMeldDisplayName(ScoringManager.MeldKind kind)
    {
        return kind switch
        {
            ScoringManager.MeldKind.Chow => "Chow",
            ScoringManager.MeldKind.Pung => "Pung",
            ScoringManager.MeldKind.Kong => "Kong",
            ScoringManager.MeldKind.Eyes => "Eye",
            _ => "Meld"
        };
    }

    private static string GetBonusDisplayName(MahjongTileData tileData)
    {
        if (tileData == null) return "Bonus";

        return tileData.TileType switch
        {
            TileType.Flower => "Flower\nBonus",
            TileType.Season => "Season\nBonus",
            _ => "Bonus"
        };
    }

    private static string BuildPopupText(string handType, int scoreValue, Color handTypeColor, Color scoreColor)
    {
        string typeHex = ColorUtility.ToHtmlStringRGBA(handTypeColor);
        string scoreHex = ColorUtility.ToHtmlStringRGBA(scoreColor);
        return $"<color=#{typeHex}>{handType}</color>\n<color=#{scoreHex}>+{scoreValue}</color>";
    }
}
