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

        // HighestScoreTMP.text = playerStats.
        // MeldsScoredTMP.text = playerStats.
        // EnemiesDefeatedTMP.text = playerStats.
        // MoneySpentTMP.text = playerStats.
    }
}
