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
    public string getRandomDesc() {
        //string comp = "";
        int index = Random.Range(0, EnemyInformationGrammer.descriptionVerb.Count);
        string verb = EnemyInformationGrammer.descriptionVerb[index];

        index = Random.Range(0, EnemyInformationGrammer.descriptionNoun.Count);
        string noun = EnemyInformationGrammer.descriptionNoun[index];

        index = Random.Range(0, EnemyInformationGrammer.descriptionAdjective.Count);
        string adj = EnemyInformationGrammer.descriptionAdjective[index] + " ";
        int roll = Random.Range(0, 2);
        adj = roll == 0 ? adj : "";

        index = Random.Range(0, EnemyInformationGrammer.descriptionPrep.Count);
        string prep = " " + EnemyInformationGrammer.descriptionPrep[index];
        roll = Random.Range(0, 3);
        prep = roll == 0 ? prep : "";

        int binary = Random.Range(0, 1);
        string close = binary == 0 ? "!" : ".";
        return verb + " " + adj + noun + prep + close;
    }
}
