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
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState currentState;

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
        currentState = GameState.Draw;
        DeckManager.Instance.drawHand();
    }
    void SetState(GameState newState) { currentState = newState; }
}
