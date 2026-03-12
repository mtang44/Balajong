using UnityEngine;
using UnityEngine.UI;

public class StatsUpdater : MonoBehaviour
{
    //We Drag this in
    public GameObject discard;
    public GameObject score;
    public GameObject healthSlider;
    // We probably want this
    public static StatsUpdater Instance;
    private void Awake()
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
}
