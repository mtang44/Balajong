using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Start,
    Draw,
    Select,
    Discard,
    Score,
    End
}
// This will govern the overall backend of the game.
public class GameManager : MonoBehaviour
{
    //THIS IS A TEMP WORKING SOLUTION BUT MAY STAY
    public GameObject encounterFlowManager;
    public static GameManager Instance;
    //governs state interest
    public GameState currentState;
    public bool selecting = false;

    //governs values of the game, will be used for determining the state to switch to.
    // Constants will normally be gathered when the game is started. For now, hard coded.
    public int maxDiscards = 3;
    public int currentDiscards = 0;
    public int score = 0;

    [Header("Action Buttons")]
    [SerializeField] private Button discardButton;
    [SerializeField] private Button checkRackButton;
    [SerializeField] private CanvasGroup discardButtonCanvasGroup;
    [SerializeField] private CanvasGroup checkRackButtonCanvasGroup;
    [SerializeField, Range(0f, 1f)] private float enabledButtonAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float disabledButtonAlpha = 0.45f;

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
        BeginGame();
    }

    void Update()
    {
        HandleRightClickDeselect();
        UpdateActionButtons();
    }

    private void HandleRightClickDeselect()
    {
        if (!selecting || !WasRightClickThisFrame())
        {
            return;
        }

        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null || deckManager.selectedTiles == null || deckManager.selectedTiles.Count == 0)
        {
            return;
        }

        deckManager.ClearSelectedTiles();
    }

    private void UpdateActionButtons()
    {
        bool hasSelection = HasAnySelectedTile();
        bool canUseActions = selecting;

        bool canDiscard = canUseActions && hasSelection;
        bool canCheckRack = canUseActions && !hasSelection;

        ApplyButtonState(discardButton, ref discardButtonCanvasGroup, canDiscard);
        ApplyButtonState(checkRackButton, ref checkRackButtonCanvasGroup, canCheckRack);
    }

    private void ApplyButtonState(Button button, ref CanvasGroup canvasGroup, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = interactable;

        if (canvasGroup == null)
        {
            canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }
        }

        float enabledAlpha = Mathf.Clamp01(enabledButtonAlpha);
        float disabledAlpha = Mathf.Clamp01(disabledButtonAlpha);
        canvasGroup.alpha = interactable ? enabledAlpha : disabledAlpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private static bool HasAnySelectedTile()
    {
        DeckManager deckManager = DeckManager.Instance;
        return deckManager != null && deckManager.selectedTiles != null && deckManager.selectedTiles.Count > 0;
    }

    private static bool WasRightClickThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    void OnSceneLoaded()
    {
        BeginGame();
    }
    void BeginGame()
    {
        StatsUpdater.Instance.UpdateHealth(PlayerStatManager.Instance.currentHealth, PlayerStatManager.Instance.maxHealth);
        StatsUpdater.Instance.UpdateDiscardCount();
        StatsUpdater.Instance.UpdateScore(0);
        StatsUpdater.Instance.UpdateScoreThreshold(EnemyManager.Instance.returnScoreThreshold());
        Debug.Log("Game Started. Current State: " + currentState);
        DeckManager.Instance.forceNewLists();
        SwitchState(GameState.Start);
    }
    void SetState(GameState newState) { currentState = newState; }
    void SwitchState(GameState newState)
    {
        SetState(newState);
        switch (currentState)
        {
            case GameState.Start:
                StartState();
                break;
            case GameState.Draw:
                DrawState();
                break;
            case GameState.Select:
                SelectState();
                break;
            case GameState.Discard:
                DiscardState();
                break;
            case GameState.Score:
                ScoreState();
                break;
            case GameState.End:
                EndState();
                break;
        }
    }
    void StartState()
    {
        SwitchState(GameState.Draw);
    }
    void DrawState()
    {
        DeckManager.Instance.redrawHand();
        SwitchState(GameState.Select);
    }
    void SelectState()
    {
        selecting = true;
    }
    void DiscardState()
    {
        DeckManager.Instance.selectedToDiscard();
        currentDiscards++;
        StatsUpdater.Instance.UpdateDiscardCount();
        if (currentDiscards < maxDiscards)
            SwitchState(GameState.Draw);
        else
            DeckManager.Instance.redrawHand();
    }
    void ScoreState()
    {
        StartCoroutine(ScoreStateCoroutine());
    }

    System.Collections.IEnumerator ScoreStateCoroutine()
    {
        Debug.Log("Scoring Hand...");
        int handScore = ScoringManager.Instance.CalcHandScore(DeckManager.Instance.getHandAsMahjongTileData());
        Debug.Log($"ScoreState: Hand scored {handScore} points.");

        // Animate the score rising tile-by-tile before applying the final total
        if (ScoreVisualization.Instance != null)
            yield return ScoreVisualization.Instance.AnimateScore(score);

        score += handScore;
        StatsUpdater.Instance.UpdateScore(score);

        bool reachedThreshold = score >= EnemyManager.Instance.returnScoreThreshold();
        if (reachedThreshold)
        {
            PlayerStatManager.Instance.cash += 5;
            StatsUpdater.Instance.UpdateCash(PlayerStatManager.Instance.cash);

            // On win, ResolveEncounterWin handles endRound/discard and scene transition.
            // Do not transition back to Draw here, or a new hand is dealt before leaving.
            currentDiscards = 0;
            StatsUpdater.Instance.UpdateDiscardCount();
            encounterFlowManager.GetComponent<MapEncounterResultHandler>().ResolveEncounterWin();
            yield break;
        }

        // On failed check, keep current hand/bonus tiles and continue selecting.
        PlayerDamage();

        currentDiscards = 0;
        StatsUpdater.Instance.UpdateDiscardCount();
        SwitchState(GameState.Select);
    }
    void EndState() {}

    void PlayerDamage()
    {
        PlayerStatManager.Instance.TakeDamage(1);
        StatsUpdater.Instance.UpdateHealth(PlayerStatManager.Instance.currentHealth, PlayerStatManager.Instance.maxHealth);
        if (PlayerStatManager.Instance.currentHealth <= 0)
        {
            SwitchState(GameState.End);
            Loss();
        }
    }
    void Loss()
    {
        Debug.Log("Player has lost the game.");
        StatsUpdater.Instance.ShowLoseScreen();
    }

    // Public method for UI button to trigger discard
    public void OnDiscardButtonPressed()
    {
        if (selecting && HasAnySelectedTile())
        {
            selecting = false;
            SwitchState(GameState.Discard);
        }
    }
    public void OnScoreButtonPressed()
    {
        if (selecting && !HasAnySelectedTile())
        {
            SwitchState(GameState.Score);
        }
    }
}
