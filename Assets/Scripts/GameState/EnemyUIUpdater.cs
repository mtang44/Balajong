using UnityEngine;

public class EnemyUIUpdater : MonoBehaviour
{
    //these gameobjects are really the text
    public GameObject enemyName;
    public GameObject enemyScore;
    public GameObject enemyDesc;

    public void Start()
    {
        UpdateEnemy();
    }
    public void UpdateEnemy()
    {
        if (enemyName != null)
        {
            TMPro.TextMeshProUGUI textComponent = enemyScore.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "" + EnemyManager.Instance.returnScoreThreshold();
        }
        if (enemyName != null)
        {
            TMPro.TextMeshProUGUI textComponent = enemyName.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "" + EnemyManager.Instance.returnName();
        }
        if (enemyName != null)
        {
            TMPro.TextMeshProUGUI textComponent = enemyDesc.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "This Guy is a LOSER!";
        }
    }
}
