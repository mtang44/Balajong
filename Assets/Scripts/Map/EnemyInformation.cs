using UnityEngine;
using System.Collections.Generic;

public class EnemyInformation
{
    public int EnemyHealth;
    public int EnemyPayout;
    public string EnemyName;

    public EnemyInformation()
    {
        EnemyHealth = 100;
        EnemyPayout = 5;
        EnemyName = "Unknown";
    }
    public EnemyInformation(MapNodeType type, int enemiesDefeatedBeforeEncounter)
    {
        EnemyName = getRandomName(type);
        switch (type)
        {
            case MapNodeType.Battle: {EnemyPayout = 5; break;}
            case MapNodeType.Elite: {EnemyPayout = 7; break;}
            case MapNodeType.Boss: {EnemyPayout = 10; break;}
            default: {EnemyPayout = 5; break;}
        }
        EnemyHealth = CalculateEnemyHealth(type, enemiesDefeatedBeforeEncounter);
    }
    public EnemyInformation(MapNodeType type)
    {
        EnemyName = getRandomName(type);
        switch (type)
        {
            case MapNodeType.Battle: {EnemyPayout = 5; break;}
            case MapNodeType.Elite: {EnemyPayout = 7; break;}
            case MapNodeType.Boss: {EnemyPayout = 10; break;}
            default: {EnemyPayout = 5; break;}
        }
        EnemyHealth = 100;
    }
    public static string getRandomBasicName()
    {
        string firstName;
        string lastName;
        int index = Random.Range(0, EnemyInformationGrammer.firstNames.Count);
        firstName = EnemyInformationGrammer.firstNames[index];
        index = Random.Range(0, EnemyInformationGrammer.lastNames.Count);
        lastName = EnemyInformationGrammer.lastNames[index];
        return firstName + " " + lastName;
    }
    public static string getRandomEliteName()
    {
        string firstName;
        string lastName;
        string title;
        int index = Random.Range(0, EnemyInformationGrammer.firstNames.Count);
        firstName = EnemyInformationGrammer.firstNames[index];
        index = Random.Range(0, EnemyInformationGrammer.lastNames.Count);
        lastName = EnemyInformationGrammer.lastNames[index];
        index = Random.Range(0, EnemyInformationGrammer.titles.Count);
        title = EnemyInformationGrammer.titles[index];
        return title + " " + firstName + " " + lastName;
    }
    public static string getRandomBossName()
    {
        int index = Random.Range(0, EnemyInformationGrammer.bossNames.Count);
        return EnemyInformationGrammer.bossNames[index];
    }
    public static string getRandomName(MapNodeType type)
    {
        switch (type) {
            case MapNodeType.Battle: return getRandomBasicName();
            case MapNodeType.Elite: return getRandomEliteName();
            case MapNodeType.Boss: return getRandomBossName();
            default: return "??? ???";
        }
    }
    public string getHealthString() { return "" + EnemyHealth; }
    public string getPayoutString() { return "$" + EnemyPayout; }

    public static int CalculateEnemyHealth(MapNodeType type, int enemiesDefeatedBeforeEncounter)
    {
        if (EnemyInformationGrammer.scoreThresholds == null || EnemyInformationGrammer.scoreThresholds.Count == 0)
            return 100;

        int lastThresholdIndex = EnemyInformationGrammer.scoreThresholds.Count - 1;
        int thresholdIndex = Mathf.Clamp(enemiesDefeatedBeforeEncounter, 0, lastThresholdIndex);
        int basicValue = EnemyInformationGrammer.scoreThresholds[thresholdIndex];

        // Once the final configured threshold is reached, keep reusing it for all future encounters.
        if (thresholdIndex >= lastThresholdIndex)
            return basicValue;

        if(type == MapNodeType.Elite)
            basicValue = (int)(basicValue * 1.5f);
        else if(type == MapNodeType.Boss)
            basicValue *= 2;
        return basicValue;
    }
}
