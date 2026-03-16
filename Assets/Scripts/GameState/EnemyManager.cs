using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //EnemyManager will deal with generating the correct score threshold.
    //Right now it is 100.
    public static EnemyManager Instance;
    public EnemyInformation enemyInfo;

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
        enemyInfo = new EnemyInformation();
    }
    public int returnScoreThreshold() { return enemyInfo.EnemyHealth; }
    public int returnPayout() { return enemyInfo.EnemyPayout; }
    public string returnName() { return enemyInfo.EnemyName; }
    public string getHealthString() { return "" + enemyInfo.EnemyHealth; }
    public string getPayoutString() { return "$" + enemyInfo.EnemyPayout; }
}
