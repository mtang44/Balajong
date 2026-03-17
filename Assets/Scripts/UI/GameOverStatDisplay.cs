using TMPro;
using UnityEngine;

public class GameOverStatDisplay : MonoBehaviour
{

    [SerializeField] TMP_Text HighestScoreTMP;
    [SerializeField] TMP_Text MeldsScoredTMP;
    [SerializeField] TMP_Text EnemiesDefeatedTMP;

    [SerializeField] TMP_Text MoneySpentTMP;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        UpdateDisplay();
    }
    public void UpdateDisplay()
    {
        PlayerStatManager playerStats = PlayerStatManager.Instance;
        if (playerStats == null) return;

        HighestScoreTMP.text = playerStats.highestScoringRack.ToString();
        MeldsScoredTMP.text = playerStats.totalMeldsScored.ToString();
        EnemiesDefeatedTMP.text = playerStats.enemiesDefeated.ToString();
        MoneySpentTMP.text = "$" + playerStats.totalMoneySpent.ToString();
    }
}
