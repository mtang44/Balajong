using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StatsUpdater : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int startupRefreshFrames = 3;

    //We Drag this in
    public GameObject discard;
    public GameObject score;
    public GameObject scoreThreshold;
    public GameObject healthSlider;
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject cash;
    public GameObject jokerCount;
    public GameObject deckCount;
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
        if (GameManager.Instance == null)
        {
            return;
        }

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
    public void UpdateJokerCount()
    {
        if (jokerCount != null)
        {
            TMPro.TextMeshProUGUI textComponent = jokerCount.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                int currentJokers = 0;
                int maxJokers = 0;

                if (JokerManager.Instance != null)
                {
                    if (JokerManager.Instance.jokers != null)
                    {
                        currentJokers = JokerManager.Instance.jokers.Count;
                    }

                    maxJokers = JokerManager.Instance.maxJokers;
                }

                textComponent.text = currentJokers + "/" + maxJokers;
            }
        }
    }

    public void UpdateDeckCount()
    {
        if (deckCount == null)
        {
            return;
        }

        TMPro.TextMeshProUGUI textComponent = deckCount.GetComponent<TMPro.TextMeshProUGUI>();
        if (textComponent == null)
        {
            return;
        }

        int currentDeckTiles = 0;
        int totalDeckTiles = 0;

        DeckManager deckManager = DeckManager.Instance;
        if (deckManager != null)
        {
            currentDeckTiles = deckManager.deck != null ? deckManager.deck.GetDeckCount() : 0;

            int handCount = deckManager.hand != null ? deckManager.hand.Count : 0;
            int flowerCount = deckManager.flowerTiles != null ? deckManager.flowerTiles.Count : 0;
            int seasonCount = deckManager.seasonTiles != null ? deckManager.seasonTiles.Count : 0;
            int discardCount = deckManager.discard != null ? deckManager.discard.Count : 0;

            totalDeckTiles = currentDeckTiles + handCount + flowerCount + seasonCount + discardCount;
        }

        textComponent.text = currentDeckTiles + "/" + totalDeckTiles;
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

    private void Start()
    {
        StartCoroutine(RefreshStatsOnSceneStart());
    }

    private IEnumerator RefreshStatsOnSceneStart()
    {
        // Refresh multiple times across early frames to handle scene object init order.
        int frames = Mathf.Max(1, startupRefreshFrames);
        for (int i = 0; i < frames; i++)
        {
            RefreshAllStatsFromManagers();
            yield return null;
        }

        RefreshAllStatsFromManagers();
    }

    private void RefreshAllStatsFromManagers()
    {
        if (PlayerStatManager.Instance != null)
        {
            UpdateHealth(PlayerStatManager.Instance.currentHealth, PlayerStatManager.Instance.maxHealth);
            UpdateCash(PlayerStatManager.Instance.cash);
        }

        if (GameManager.Instance != null)
        {
            UpdateDiscardCount();
            UpdateScore(GameManager.Instance.score);
        }

        if (EnemyManager.Instance != null)
        {
            UpdateScoreThreshold(EnemyManager.Instance.returnScoreThreshold());
        }

        UpdateJokerCount();
        UpdateDeckCount();
    }
}
