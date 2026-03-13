using UnityEngine;
using UnityEngine.UI;

public class StatsUpdater : MonoBehaviour
{
    //We Drag this in
    public GameObject discard;
    public GameObject score;
    public GameObject scoreThreshold;
    public GameObject healthSlider;
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject cash;
    // We probably want this
    public static StatsUpdater Instance;
    private void Awake()
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
    public void UpdateDiscardCount()
    {
        int count = GameManager.Instance.maxDiscards - GameManager.Instance.currentDiscards;
        if (discard != null)
        {
            TMPro.TextMeshProUGUI textComponent = discard.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "" + count;
            }
        }
    }
    public void UpdateCash(int cashValue)
    {
        if (cash != null)
        {
            TMPro.TextMeshProUGUI textComponent = cash.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "$" + cashValue;
            }
        }
    }
    public void UpdateScore(int scoreValue)
    {
        if (score != null)
        {
            TMPro.TextMeshProUGUI textComponent = score.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "" + scoreValue;
            }
        }
    }
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            UnityEngine.UI.Slider sliderComponent = healthSlider.GetComponent<UnityEngine.UI.Slider>();
            if (sliderComponent != null)
            {
                sliderComponent.maxValue = maxHealth;
                sliderComponent.value = currentHealth;
            }
        }
    }
    public void UpdateScoreThreshold(int scoreThreshold)
    {
        if (this.scoreThreshold != null)
        {
            TMPro.TextMeshProUGUI textComponent = this.scoreThreshold.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "" + scoreThreshold;
            }
        }
    }
    public void ShowWinScreen()
    {
        if (winScreen != null)
            winScreen.SetActive(true);
    }
    public void ShowLoseScreen()
    {
        if (loseScreen != null)
            loseScreen.SetActive(true);
    }
}
