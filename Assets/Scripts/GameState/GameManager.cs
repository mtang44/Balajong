using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

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
    public static GameManager Instance;
    //governs state interest
    public GameState currentState;
    public bool selecting = false;

    //governs values of the game, will be used for determining the state to switch to.
    // Constants will normally be gathered when the game is started. For now, hard coded.
    public int maxDiscards = 3;
    public int currentDiscards = 0;

    void Awake()
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
    void Start()
    {
        currentState = GameState.Start;
        SwitchState(currentState);
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
        Debug.Log("Scoring Hand...");
        int Score = ScoringManager.Instance.CalcHandScore(DeckManager.Instance.getHandAsMahjongTileData());
        Debug.Log($"ScoreState: Hand scored {Score} points.");
        StatsUpdater.Instance.UpdateScore(Score);
        
        // Here, we decide if the player is alive or not. For now, we will return to the draw state and refill discards.
        currentDiscards = 0;
        StatsUpdater.Instance.UpdateDiscardCount();
        SwitchState(GameState.Draw);
    }
    void EndState() {}

    // Public method for UI button to trigger discard
    public void OnDiscardButtonPressed()
    {
        if (selecting)
        {
            selecting = false;
            SwitchState(GameState.Discard);
        }
    }
    public void OnScoreButtonPressed()
    {
        SwitchState(GameState.Score);
    }
}
