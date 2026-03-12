using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //EnemyManager will deal with generating the correct score threshold.
    //Right now it is 100.
    int baseValue = 100;
    public static EnemyManager Instance;

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
    public int returnScoreThreshold()
    {
        //This is where we will eventually calculate the score threshold based on the current game state.
        return baseValue;
    }
}
