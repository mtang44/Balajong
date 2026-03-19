using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public enum ScoreSound
{
    Small,
    Medium,
    Large
}
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
    [SerializeField, Min(0.1f)] private float singlePopupTextSizeMultiplier = 0.75f;
    [SerializeField, Min(0.1f)] private float balajongPopupTextSizeMultiplier = 1.25f;
    [SerializeField, Min(0.1f)] private float editionTextSizeMultiplier = 0.65f;
    [SerializeField] private Color handTypeTextColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color editionTextColor = new Color(0.8f, 0.55f, 1f, 1f);
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
    [SerializeField, Min(0f)] private float singleTimingMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float scoreLerpDuration = 0.2f;
    [SerializeField, Min(0f)] private float pauseAfterEditionPopup = 0.18f;
    [SerializeField, Min(0f)] private float pauseBeforeScore = 0.15f;

    [Header("Tile Hop")]
    [SerializeField, Min(0f)] private float tileHopHeight = 0.2f;
    [SerializeField, Min(0.05f)] private float tileHopDuration = 0.22f;
    [SerializeField, Min(0f)] private float tileHopSquashAmount = 0.05f;
    [SerializeField, Min(0f)] private float tileHopStretchAmount = 0.06f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Animate the score readout starting from <paramref name="baseScore"/> and land at <paramref name="finalScore"/>.
    // Yield on this coroutine from GameManager; it resolves when the animation finishes.
    public IEnumerator AnimateScore(int baseScore, int finalScore)
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

            yield return AnimateFinalScoreDelta(runningScore, finalScore);

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
        Dictionary<int, List<int>> triggerHopIndices = new Dictionary<int, List<int>>();
        Dictionary<int, ScoringManager.MeldKind> triggerKinds = new Dictionary<int, ScoringManager.MeldKind>();
        Dictionary<int, ScoringManager.Meld> triggerMelds = new Dictionary<int, ScoringManager.Meld>();
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
            triggerHopIndices[triggerIndex] = new List<int>(meldIndices);
            triggerKinds[triggerIndex] = meld.Kind;
            triggerMelds[triggerIndex] = meld;
        }

        // Go along the hand left to right, showing popups for melds
        for (int i = 0; i < hand.Count; i++)
        {
            if (slotScore[i] < 0)
                continue; // not in any meld – no delay needed

            bool hasTriggerKind = triggerKinds.TryGetValue(i, out ScoringManager.MeldKind triggerKind);
            bool isSingleTrigger =
                slotScore[i] > 0 &&
                hasTriggerKind &&
                triggerKind == ScoringManager.MeldKind.Single;

            List<Edition> triggerEditions = null;
            ScoringManager.Meld triggerMeld = default;
            bool hasTriggerMeld = false;
            bool hasNonBaseEditions = false;
            if (slotScore[i] > 0 && triggerMelds.TryGetValue(i, out triggerMeld))
            {
                hasTriggerMeld = true;
                triggerEditions = GetNonBaseEditions(triggerMeld);
                hasNonBaseEditions = triggerEditions.Count > 0;
            }

            float triggerTimingMultiplier =
                (isSingleTrigger && !hasNonBaseEditions)
                    ? singleTimingMultiplier
                    : 1f;

            float stepDelay = delayPerTile * triggerTimingMultiplier;
            if (stepDelay > 0f)
            {
                yield return new WaitForSeconds(stepDelay);
            }

            if (slotScore[i] > 0) // trigger tile: fire the popup
            {
                if (hasTriggerMeld)
                {
                    TriggerMeldJokerShakes(triggerMeld);
                }

                if (triggerHopIndices.TryGetValue(i, out List<int> hopIndices))
                {
                    TriggerMeldHop(hand, hopIndices);
                }

                Vector3 spawnPos = hand[i].transform.position + popupWorldOffset;
                string handType = string.IsNullOrEmpty(slotTypeLabel[i]) ? "Meld" : slotTypeLabel[i];
                float popupTextSizeMultiplier = hasTriggerKind ? GetMeldPopupTextSizeMultiplier(triggerKind) : 1f;

                // 1. Meld type label
                SpawnRawTextPopup(spawnPos, handType, handTypeTextColor, popupTextSizeMultiplier);

                // 2. Non-base edition labels, one after another
                if (triggerEditions != null)
                {
                    foreach (Edition ed in triggerEditions)
                    {
                        yield return new WaitForSeconds(pauseAfterEditionPopup);
                        SpawnRawTextPopup(spawnPos, GetEditionDisplayName(ed), editionTextColor, editionTextSizeMultiplier);
                    }
                }

                // Always leave a beat before score so type/score never overlap.
                float preScorePause = pauseBeforeScore * triggerTimingMultiplier;
                if (preScorePause > 0f)
                {
                    yield return new WaitForSeconds(preScorePause);
                }

                // 3. Score popup
                SpawnRawTextPopup(spawnPos, $"+{slotScore[i]}", scoreTextColor, popupTextSizeMultiplier);

                float pauseDuration = pauseAfterMeld * triggerTimingMultiplier;
                yield return LerpScoreAndWait(runningScore, slotScore[i], pauseDuration);
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

        yield return AnimateFinalScoreDelta(runningScore, finalScore);
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

            TriggerTileHop(tile);
            playScoreSound(1);

            Vector3 spawnPos = tile.transform.position + popupWorldOffset;
            string handType = GetBonusDisplayName(td);
            SpawnPopup(spawnPos, handType, bonus, handTypeTextColor, scoreTextColor);

            float waitAfterBonus = pauseAfterBonus;
            if (IsTileHopEnabled())
            {
                waitAfterBonus = Mathf.Max(waitAfterBonus, tileHopDuration);
            }

            yield return LerpScoreAndWait(runningScore, bonus, waitAfterBonus);
        }
    }

    private IEnumerator LerpScoreAndWait(int[] runningScore, int scoreDelta, float totalWaitDuration)
    {
        if (runningScore == null || runningScore.Length == 0)
        {
            yield break;
        }

        int startScore = runningScore[0];
        int targetScore = ScoreMath.SaturatingAdd(startScore, scoreDelta);
        float lerpDuration = Mathf.Max(0f, scoreLerpDuration);
        if (totalWaitDuration > 0f)
        {
            lerpDuration = Mathf.Min(lerpDuration, totalWaitDuration);
        }

        if (lerpDuration <= 0f || startScore == targetScore)
        {
            runningScore[0] = targetScore;
            StatsUpdater.Instance.UpdateScore(runningScore[0]);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < lerpDuration)
            {
                float t = elapsed / lerpDuration;
                int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
                if (displayScore != runningScore[0])
                {
                    runningScore[0] = displayScore;
                    StatsUpdater.Instance.UpdateScore(runningScore[0]);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            runningScore[0] = targetScore;
            StatsUpdater.Instance.UpdateScore(runningScore[0]);
        }

        float remainingWait = totalWaitDuration - lerpDuration;
        if (remainingWait > 0f)
        {
            yield return new WaitForSeconds(remainingWait);
        }
    }

    private IEnumerator AnimateFinalScoreDelta(int[] runningScore, int finalScore)
    {
        if (runningScore == null || runningScore.Length == 0)
            yield break;

        if (runningScore[0] == finalScore)
            yield break;

        HashSet<string> endRoundJokers = GetRoundEndJokersThatAffectFinalScore();
        if (endRoundJokers.Count > 0)
        {
            JokerManager.Instance?.ShakeJokers(endRoundJokers);
        }

        float finalLerpDuration = Mathf.Max(scoreLerpDuration, 0.35f);
        yield return LerpScoreToTarget(runningScore, finalScore, finalLerpDuration);
    }

    private IEnumerator LerpScoreToTarget(int[] runningScore, int targetScore, float duration)
    {
        if (runningScore == null || runningScore.Length == 0)
            yield break;

        int startScore = runningScore[0];
        if (startScore == targetScore)
            yield break;

        float lerpDuration = Mathf.Max(0f, duration);
        if (lerpDuration <= 0f)
        {
            runningScore[0] = targetScore;
            StatsUpdater.Instance.UpdateScore(runningScore[0]);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            float t = Mathf.Clamp01(elapsed / lerpDuration);
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
            if (displayScore != runningScore[0])
            {
                runningScore[0] = displayScore;
                StatsUpdater.Instance.UpdateScore(runningScore[0]);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        runningScore[0] = targetScore;
        StatsUpdater.Instance.UpdateScore(runningScore[0]);
    }

    private void TriggerMeldJokerShakes(ScoringManager.Meld meld)
    {
        JokerManager jokerManager = JokerManager.Instance;
        if (jokerManager == null)
            return;

        HashSet<string> impactingJokers = GetMeldImpactingJokers(meld);
        if (impactingJokers.Count > 0)
        {
            jokerManager.ShakeJokers(impactingJokers);
        }
    }

    private HashSet<string> GetMeldImpactingJokers(ScoringManager.Meld meld)
    {
        HashSet<string> impacting = new HashSet<string>();
        if (meld.Tiles == null)
            return impacting;

        bool hasDots = false;
        bool hasBam = false;
        bool hasCrack = false;
        bool hasWind = false;
        bool hasDragon = false;
        bool hasCrystal = false;
        bool hasEnchanted = false;
        bool hasGhost = false;

        foreach (MahjongTileData tile in meld.Tiles)
        {
            if (tile == null)
                continue;

            switch (tile.TileType)
            {
                case TileType.Dots:
                    hasDots = true;
                    break;
                case TileType.Bam:
                    hasBam = true;
                    break;
                case TileType.Crack:
                    hasCrack = true;
                    break;
                case TileType.Wind:
                    hasWind = true;
                    break;
                case TileType.Dragon:
                    hasDragon = true;
                    break;
            }

            if (tile.Edition == Edition.Crystal)
                hasCrystal = true;
            if (tile.Edition == Edition.Enchanted)
                hasEnchanted = true;
            if (tile.Edition == Edition.Ghost)
                hasGhost = true;
        }

        if (hasDots) AddIfActive(impacting, "blue");
        if (hasBam) AddIfActive(impacting, "green");
        if (hasCrack) AddIfActive(impacting, "red");
        if (hasDots) AddIfActive(impacting, "polar");
        if (hasCrack) AddIfActive(impacting, "grizzly");
        if (hasBam) AddIfActive(impacting, "panda");
        if (hasWind) AddIfActive(impacting, "secondwind");

        if (hasCrystal && HasActivation("rainbow"))
            impacting.Add("rainbow");
        if (hasEnchanted && HasActivation("unbreaking"))
            impacting.Add("unbreaking");
        if (hasGhost && HasActivation("grave"))
            impacting.Add("grave");
        if (hasDragon && HandContainsWind() && HasActivation("ancalagon"))
            impacting.Add("ancalagon");

        switch (meld.Kind)
        {
            case ScoringManager.MeldKind.Eyes:
                AddIfActive(impacting, "eyes");
                break;
            case ScoringManager.MeldKind.Pung:
                AddIfActive(impacting, "three");
                break;
            case ScoringManager.MeldKind.Kong:
            case ScoringManager.MeldKind.Quint:
            case ScoringManager.MeldKind.Balajong:
                AddIfActive(impacting, "clover");
                break;
        }

        return impacting;
    }

    private HashSet<string> GetRoundEndJokersThatAffectFinalScore()
    {
        HashSet<string> impacting = new HashSet<string>();
        JokerManager jokerManager = JokerManager.Instance;
        if (jokerManager == null)
            return impacting;

        if (HasActivation("bagged") && jokerManager.baggedJokerBuff > 0)
            impacting.Add("bagged");
        if (HasActivation("fishdish"))
            impacting.Add("fishdish");
        if (HasActivation("ledger"))
            impacting.Add("ledger");
        if (HasActivation("joker"))
            impacting.Add("joker");
        if (HasActivation("hatcat"))
            impacting.Add("hatcat");
        if (HasActivation("knight") && jokerManager.knightJokerBuff > 0)
            impacting.Add("knight");

        PlayerStatManager stats = PlayerStatManager.Instance;
        if (HasActivation("spider") && stats != null && (stats.maxHealth - stats.currentHealth) > 0)
            impacting.Add("spider");

        return impacting;
    }

    private static bool HasActivation(string jokerCode)
    {
        return JokerManager.Instance != null && JokerManager.Instance.numberOfActivations(jokerCode) > 0;
    }

    private static void AddIfActive(HashSet<string> impacting, string jokerCode)
    {
        if (HasActivation(jokerCode))
            impacting.Add(jokerCode);
    }

    private static bool HandContainsWind()
    {
        DeckManager deck = DeckManager.Instance;
        if (deck == null || deck.hand == null)
            return false;

        foreach (GameObject tile in deck.hand)
        {
            MahjongTileData tileData = tile != null ? tile.GetComponent<MahjongTileHolder>()?.TileData : null;
            if (tileData != null && tileData.TileType == TileType.Wind)
                return true;
        }

        return false;
    }

    private void TriggerMeldHop(List<GameObject> hand, List<int> meldIndices)
    {
        if (hand == null || meldIndices == null || meldIndices.Count == 0)
        {
            return;
        }

        HashSet<int> uniqueIndices = new HashSet<int>();
        foreach (int idx in meldIndices)
        {
            if (!uniqueIndices.Add(idx))
            {
                continue;
            }

            if (idx < 0 || idx >= hand.Count)
            {
                continue;
            }

            GameObject tile = hand[idx];
            if (tile == null)
            {
                continue;
            }

            TriggerTileHop(tile);
            playScoreSound(meldIndices.Count);
        }
    }
    public void playScoreSound(int numTiles)
    {
        ScoreSound type = ScoreSound.Small;
        if(numTiles >= 3) type = ScoreSound.Medium;
        if(numTiles >= 5) type = ScoreSound.Large;
        switch (type)
        {
            case ScoreSound.Small: { SoundManager.Instance.playSmallScoreSound(); break; }
            case ScoreSound.Medium: { SoundManager.Instance.playMediumScoreSound(); break; }
            case ScoreSound.Large: { SoundManager.Instance.playBigScoreSound(); break; }
            default: { SoundManager.Instance.playSmallScoreSound(); break; }
        }
    }

    private void TriggerTileHop(GameObject tile)
    {
        if (tile == null)
        {
            return;
        }

        if (!IsTileHopEnabled())
        {
            return;
        }

        StartCoroutine(AnimateTileHop(tile));
    }

    private bool IsTileHopEnabled()
    {
        return tileHopHeight > 0f && tileHopDuration > 0f;
    }

    private IEnumerator AnimateTileHop(GameObject tile)
    {
        if (tile == null)
        {
            yield break;
        }

        Transform tileTransform = tile.transform;
        Vector3 startPos = tileTransform.position;
        Vector3 startScale = tileTransform.localScale;
        float duration = Mathf.Max(0.01f, tileHopDuration);
        float elapsed = 0f;

        while (elapsed < duration && tileTransform != null)
        {
            float t = elapsed / duration;
            float arc = Mathf.Sin(t * Mathf.PI);
            float hop = arc * tileHopHeight;
            tileTransform.position = startPos + Vector3.up * hop;

            // Keep this subtle for 3D tiles: squash near takeoff/landing, stretch near apex.
            float edge = 1f - arc;
            float yScale = 1f + (tileHopStretchAmount * arc) - (tileHopSquashAmount * edge);
            float xzScale = 1f - (tileHopStretchAmount * 0.5f * arc) + (tileHopSquashAmount * 0.5f * edge);

            yScale = Mathf.Max(0.01f, yScale);
            xzScale = Mathf.Max(0.01f, xzScale);
            tileTransform.localScale = new Vector3(startScale.x * xzScale, startScale.y * yScale, startScale.z * xzScale);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (tileTransform != null)
        {
            tileTransform.position = startPos;
            tileTransform.localScale = startScale;
        }
    }

    private void SpawnRawTextPopup(Vector3 worldPos, string text, Color textColor, float textSizeMultiplier = 1f)
    {
        GameObject go = new GameObject("ScorePopup");
        Camera cam = Camera.main;
        go.transform.position = ClampWorldPositionX(worldPos, cam);

        if (cam != null)
            go.transform.rotation = cam.transform.rotation;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        string colorHex = ColorUtility.ToHtmlStringRGBA(textColor);
        tmp.text = $"<color=#{colorHex}>{text}</color>";
        tmp.fontSize = fontSize * Mathf.Max(0.1f, textSizeMultiplier);
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        if (fontAsset != null) tmp.font = fontAsset;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 100;

        StartCoroutine(AnimatePopup(go));
    }

    private void SpawnPopup(Vector3 worldPos, string handType, int scoreValue, Color handTypeColor, Color scoreColor, float textSizeMultiplier = 1f)
    {
        GameObject go = new GameObject("ScorePopup");
        Camera cam = Camera.main;
        go.transform.position = ClampWorldPositionX(worldPos, cam);

        // Billboard: copy camera rotation so text always faces the player
        if (cam != null)
            go.transform.rotation = cam.transform.rotation;

        TextMeshPro tmp      = go.AddComponent<TextMeshPro>();
        tmp.text             = BuildPopupText(handType, scoreValue, handTypeColor, scoreColor);
        float resolvedMultiplier = Mathf.Max(0.1f, textSizeMultiplier);
        tmp.fontSize         = fontSize * resolvedMultiplier;
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

    private float GetMeldPopupTextSizeMultiplier(ScoringManager.MeldKind kind)
    {
        return kind switch
        {
            ScoringManager.MeldKind.Single => singlePopupTextSizeMultiplier,
            ScoringManager.MeldKind.Balajong => balajongPopupTextSizeMultiplier,
            _ => 1f
        };
    }

    private static string GetMeldDisplayName(ScoringManager.MeldKind kind)
    {
        return kind switch
        {
            ScoringManager.MeldKind.Single => "Single",
            ScoringManager.MeldKind.Chow => "Chow",
            ScoringManager.MeldKind.Jog => "Jog",
            ScoringManager.MeldKind.Sprint => "Sprint",
            ScoringManager.MeldKind.Pung => "Pung",
            ScoringManager.MeldKind.Kong => "Kong",
            ScoringManager.MeldKind.Quint => "Quint",
            ScoringManager.MeldKind.Balajong => "BALAJONG",
            ScoringManager.MeldKind.Eyes => "Eye",
            ScoringManager.MeldKind.Hydra => "Hydra",
            ScoringManager.MeldKind.News => "NEWS",
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

    private static List<Edition> GetNonBaseEditions(ScoringManager.Meld meld)
    {
        HashSet<Edition> seen = new HashSet<Edition>();
        List<Edition> result = new List<Edition>();
        foreach (MahjongTileData td in meld.Tiles)
        {
            if (td != null && td.Edition != Edition.Base && seen.Add(td.Edition))
                result.Add(td.Edition);
        }
        return result;
    }

    private static string GetEditionDisplayName(Edition edition)
    {
        return edition switch
        {
            Edition.Ghost => "Ghost",
            Edition.Enchanted => "Enchanted",
            Edition.Crystal => "Crystal",
            _ => edition.ToString()
        };
    }

    private static string BuildPopupText(string handType, int scoreValue, Color handTypeColor, Color scoreColor)
    {
        string typeHex = ColorUtility.ToHtmlStringRGBA(handTypeColor);
        string scoreHex = ColorUtility.ToHtmlStringRGBA(scoreColor);
        return $"<color=#{typeHex}>{handType}</color>\n<color=#{scoreHex}>+{scoreValue}</color>";
    }
}
