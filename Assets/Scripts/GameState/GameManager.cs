using System.Collections;
using System.Diagnostics;
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
    public GameState currentState;
    public bool selecting = false;

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
        DeckManager.Instance.drawHand();
        SwitchState(GameState.Select);
    }
    void SelectState()
    {
        selecting = true;

    }
    void DiscardState() {}
    void ScoreState() {}
    void EndState() {}
}
