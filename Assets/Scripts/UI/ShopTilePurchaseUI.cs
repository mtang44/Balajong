using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

[DisallowMultipleComponent]
public class ShopTilePurchaseUI : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
{
    [Tooltip("TileSpawner that owns the spawned shop tiles.")]
    [SerializeField] private TileSpawner tileSpawner;

    [Tooltip("Slot index from TileSpawner this UI should control.")]
    [SerializeField] [Min(0)] private int slotIndex;

    [Tooltip("Button shown after clicking this slot UI.")]
    [SerializeField] private Button buyButton;

    [Tooltip("TMP_Text label to display the price.")]
    [SerializeField] private TMP_Text priceLabel;

    [Tooltip("TMP_Text label to display the tile title (for example: One Dots).")]
    [SerializeField] private TMP_Text titleLabel;

    [Tooltip("TMP_Text label to display the tile description.")]
    [SerializeField] private TMP_Text descriptionLabel;

    [Tooltip("Optional panel GameObject wrapping the price tag. Hidden when there is no tile in this slot.")]
    [SerializeField] private GameObject priceTagPanel;

    [Tooltip("Optional child object to disable after this slot is purchased. If empty, the first child is used.")]
    [SerializeField] private GameObject overlayChildToDisableOnPurchase;

    [SerializeField] private bool hideBuyButtonOnStart = true;
    [SerializeField] private bool hideBuyButtonAfterPurchase = true;
    [SerializeField] private bool hideBuyButtonOnPointerExit = true;

    private TileSpawner subscribedTileSpawner;
    private EventTrigger overlayEventTrigger;
    private bool isOverlayDisabledByPurchase;

    private void Awake()
    {
        if (buyButton == null)
        {
            buyButton = GetComponentInChildren<Button>(true);
        }

        overlayEventTrigger = GetComponent<EventTrigger>();

        ResolveTextLabelsByName();
        ResolvePriceTagPanel();
        ResolveOverlayChildToDisable();

        if (priceLabel == null)
        {
            priceLabel = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        ResolveTileSpawner();
        SubscribeToSpawnerEvents();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyCurrentSlotTile);
            buyButton.onClick.AddListener(BuyCurrentSlotTile);
            if (hideBuyButtonOnStart)
            {
                buyButton.gameObject.SetActive(false);
            }
        }

        UpdatePriceLabel();
    }

    private void OnDisable()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyCurrentSlotTile);
        }

        UnsubscribeFromSpawnerEvents();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (buyButton == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: buyButton is not assigned.", this);
            return;
        }

        if (isOverlayDisabledByPurchase)
        {
            buyButton.gameObject.SetActive(false);
            return;
        }

        ResolveTileSpawner();
        if (tileSpawner == null || tileSpawner.GetSpawnedTile(slotIndex) == null)
        {
            buyButton.gameObject.SetActive(false);
            UpdatePriceLabel();
            return;
        }

        buyButton.gameObject.SetActive(true);

        UpdatePriceLabel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hideBuyButtonOnPointerExit || buyButton == null)
        {
            return;
        }

        if (isOverlayDisabledByPurchase)
        {
            buyButton.gameObject.SetActive(false);
            return;
        }

        if (eventData != null && eventData.pointerEnter != null)
        {
            Transform enteredTransform = eventData.pointerEnter.transform;
            if (enteredTransform == transform || enteredTransform.IsChildOf(transform))
            {
                return;
            }
        }

        buyButton.gameObject.SetActive(false);
    }

    public void BuyCurrentSlotTile()
    {
        if (isOverlayDisabledByPurchase)
        {
            return;
        }

        ResolveTileSpawner();
        if (tileSpawner == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: TileSpawner not found.", this);
            return;
        }

        DeckManager deckManager = DeckManager.Instance;
        PlayerStatManager playerStats = PlayerStatManager.Instance;
        if (deckManager == null || deckManager.deck == null || playerStats == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: Missing DeckManager or PlayerStatManager.", this);
            return;
        }

        GameObject tileObj = tileSpawner.GetSpawnedTile(slotIndex);
        if (tileObj == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: No tile available to buy in this slot.", this);
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }
            UpdatePriceLabel();
            return;
        }

        MahjongTileHolder holder = tileObj.GetComponent<MahjongTileHolder>();
        if (holder == null || holder.TileData == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: Spawned tile is missing MahjongTileHolder/TileData.", this);
            UpdatePriceLabel();
            return;
        }

        int price = GetTilePrice(holder.TileData);
        if (playerStats.cash < price)
        {
            Debug.Log("Not enough cash to buy this tile.");
            return;
        }

        if (!tileSpawner.TryTakeTileData(slotIndex, out MahjongTileData purchasedTileData) || purchasedTileData == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: No tile available to buy in this slot.", this);
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }
            UpdatePriceLabel();
            return;
        }

        playerStats.cash -= price;
        playerStats.AddMoneySpent(price);
        StatsUpdater.Instance?.UpdateCash(playerStats.cash);

        deckManager.deck.AddTileAtFront(purchasedTileData);

        if (hideBuyButtonAfterPurchase && buyButton != null)
        {
            buyButton.gameObject.SetActive(false);
        }

        SetOverlayChildActive(false);
        UpdatePriceLabel();
    }

    private void UpdatePriceLabel()
    {
        if (priceLabel == null && titleLabel == null && descriptionLabel == null)
            return;

        ResolveTileSpawner();
        if (tileSpawner == null)
        {
            SetTileInfoText(string.Empty, string.Empty, string.Empty);
            SetPriceTagPanelVisible(false);
            return;
        }

        GameObject tileObj = tileSpawner.GetSpawnedTile(slotIndex);
        if (tileObj == null)
        {
            SetTileInfoText(string.Empty, string.Empty, string.Empty);
            SetPriceTagPanelVisible(false);
            return;
        }

        MahjongTileHolder holder = tileObj.GetComponent<MahjongTileHolder>();
        if (holder == null || holder.TileData == null)
        {
            SetTileInfoText(string.Empty, string.Empty, string.Empty);
            SetPriceTagPanelVisible(false);
            return;
        }

        MahjongTileData tileData = holder.TileData;
        int price = GetTilePrice(tileData);

        string titleText = tileData.GetTileDisplayName();
        string descriptionText = BuildTileDescription(tileData);
        SetTileInfoText("$" + price, titleText, descriptionText);
        SetPriceTagPanelVisible(true);
    }

    private void ResolveTextLabelsByName()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null)
            {
                continue;
            }

            if (priceLabel == null && label.name == "Price Tag TMP")
            {
                priceLabel = label;

                if (priceTagPanel == null && label.transform.parent != null)
                {
                    priceTagPanel = label.transform.parent.gameObject;
                }

                continue;
            }

            if (titleLabel == null && label.name == "Title TMP")
            {
                titleLabel = label;
                continue;
            }

            if (descriptionLabel == null && label.name == "Description TMP")
            {
                descriptionLabel = label;
            }
        }
    }

    private void ResolvePriceTagPanel()
    {
        if (priceTagPanel != null)
        {
            return;
        }

        if (priceLabel != null && priceLabel.transform.parent != null)
        {
            priceTagPanel = priceLabel.transform.parent.gameObject;
            return;
        }

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child != null && child.name == "Price Tag Panel")
            {
                priceTagPanel = child.gameObject;
                return;
            }
        }
    }

    private void ResolveOverlayChildToDisable()
    {
        if (overlayChildToDisableOnPurchase != null)
        {
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            GameObject childObject = child.gameObject;
            if (childObject == priceTagPanel)
            {
                continue;
            }

            if (buyButton != null && childObject == buyButton.gameObject)
            {
                continue;
            }

            overlayChildToDisableOnPurchase = childObject;
            return;
        }

        if (transform.childCount > 0)
        {
            overlayChildToDisableOnPurchase = transform.GetChild(0).gameObject;
        }
    }

    private void SetOverlayChildActive(bool isActive)
    {
        ResolveOverlayChildToDisable();

        if (overlayChildToDisableOnPurchase == null)
        {
            Debug.LogWarning("ShopTilePurchaseUI: No overlay child found to toggle after purchase.", this);
            return;
        }

        if (overlayChildToDisableOnPurchase.activeSelf != isActive)
        {
            overlayChildToDisableOnPurchase.SetActive(isActive);
        }

        if (overlayEventTrigger != null)
        {
            overlayEventTrigger.enabled = isActive;
        }

        isOverlayDisabledByPurchase = !isActive;
    }

    private void SetPriceTagPanelVisible(bool isVisible)
    {
        ResolvePriceTagPanel();
        if (priceTagPanel == null)
        {
            return;
        }

        if (priceTagPanel.activeSelf != isVisible)
        {
            priceTagPanel.SetActive(isVisible);
        }
    }

    private void SetTileInfoText(string priceText, string titleText, string descriptionText)
    {
        if (priceLabel != null)
        {
            priceLabel.text = priceText;
        }

        if (titleLabel != null)
        {
            titleLabel.text = titleText;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = descriptionText;
        }
    }

    private string BuildTileDescription(MahjongTileData tileData)
    {
        int faceValue = ResolveTileFaceValue(tileData);
        string pointsText = "+" + faceValue + " points";

        if (tileData == null || tileData.Edition == Edition.Base)
        {
            return pointsText;
        }

        string editionDescription = GetEditionDescription(tileData.Edition);
        if (string.IsNullOrWhiteSpace(editionDescription))
        {
            return pointsText;
        }

        return pointsText + "\n" + editionDescription;
    }

    private int ResolveTileFaceValue(MahjongTileData tileData)
    {
        if (tileData == null)
        {
            return 0;
        }

        if (ScoringManager.Instance != null)
        {
            return ScoringManager.Instance.GetTileFaceValue(tileData);
        }

        return tileData.TileType switch
        {
            TileType.Dots => (int)tileData.NumberedValue,
            TileType.Bam => (int)tileData.NumberedValue,
            TileType.Crack => (int)tileData.NumberedValue,
            TileType.Wind => ScoreTable.HonorScore,
            TileType.Dragon => ScoreTable.HonorScore,
            TileType.Flower => ScoreTable.BonusScore,
            TileType.Season => ScoreTable.BonusScore,
            _ => 0
        };
    }

    private static string GetEditionDescription(Edition edition)
    {
        return edition switch
        {
            Edition.Ghost => "Ghost: Adds +50 points to meld",
            Edition.Enchanted => "Enchanted: Multiplies meld multiplier by x1.5",
            Edition.Crystal => "Crystal: Meld multiplier gains +5",
            _ => string.Empty
        };
    }

    private int GetTilePrice(MahjongTileData tileData)
    {
        if (tileData == null)
            return 0;
        return tileData.Edition == Edition.Base ? 1 : 2;
    }

    private void ResolveTileSpawner()
    {
        if (tileSpawner != null)
        {
            return;
        }

        tileSpawner = FindFirstObjectByType<TileSpawner>();
    }

    private void SubscribeToSpawnerEvents()
    {
        if (tileSpawner == null)
        {
            return;
        }

        if (subscribedTileSpawner == tileSpawner)
        {
            return;
        }

        UnsubscribeFromSpawnerEvents();
        subscribedTileSpawner = tileSpawner;
        subscribedTileSpawner.TilesChanged += HandleTilesChanged;
    }

    private void UnsubscribeFromSpawnerEvents()
    {
        if (subscribedTileSpawner == null)
        {
            return;
        }

        subscribedTileSpawner.TilesChanged -= HandleTilesChanged;
        subscribedTileSpawner = null;
    }

    private void HandleTilesChanged()
    {
        if (isOverlayDisabledByPurchase)
        {
            ResolveTileSpawner();
            if (tileSpawner != null && tileSpawner.GetSpawnedTile(slotIndex) != null)
            {
                SetOverlayChildActive(true);
            }
        }

        UpdatePriceLabel();
    }
}
