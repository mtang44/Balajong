using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Consumable use: Consumable 1 -> Use BTN 1 (slot 0), Consumable 2 -> Use BTN 2 (slot 1). Buttons stay visible.
// Inventory lives in PlayerStatManager (2 slots, persists entire game). Shop buy adds there; Use removes after use.
public class ConsumableEffectSystem : MonoBehaviour
{
    public static ConsumableEffectSystem Instance { get; private set; }

    [Header("Scene References (optional; auto-found if null)")]
    [SerializeField] private Shop shop;
    [SerializeField] private GameObject shopRoot;
    [Tooltip("Use BTN 1: click activates consumable in slot 0 (first consumable).")]
    [SerializeField] private Button useButtonSlot0;
    [Tooltip("Use BTN 2: click activates consumable in slot 1 (second consumable).")]
    [SerializeField] private Button useButtonSlot1;
    [Tooltip("Copy BTN: shown in Add/Clone flow after selecting 1 tile. Bind OnClick to OnCopy().")]
    [SerializeField] private Button copyButton;

    private DeckManager deckManager;
    private Consumable activeConsumable;
    private int _slotIndexInUse = -1;
    private int addConsumablePhase;

    public static bool InTileSelectionPhase => Instance != null && Instance.activeConsumable != null;
    public static bool InAddDiscardPhase =>
        Instance != null && Instance.activeConsumable != null && IsAddType(Instance.activeConsumable) && Instance.addConsumablePhase == 1;
    public static bool HasFourSelected =>
        DeckManager.Instance != null && DeckManager.Instance.selectedTiles != null && DeckManager.Instance.selectedTiles.Count == 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (shop == null) shop = FindFirstObjectByType<Shop>();
        if (deckManager == null) deckManager = DeckManager.Instance ?? FindFirstObjectByType<DeckManager>();

        if (useButtonSlot0 != null)
            useButtonSlot0.onClick.AddListener(() => UseSlot(0));
        if (useButtonSlot1 != null)
            useButtonSlot1.onClick.AddListener(() => UseSlot(1));
        if (copyButton != null)
            copyButton.onClick.AddListener(OnCopy);
        if (copyButton != null)
            copyButton.gameObject.SetActive(false);
    }

    public void UseSlot0() => UseSlot(0);
    public void UseSlot1() => UseSlot(1);
    public void OnCopy()
    {
        if (activeConsumable == null || deckManager == null || !IsAddType(activeConsumable) || addConsumablePhase != 0) return;
        var sel = deckManager.selectedTiles;
        if (sel == null || sel.Count != 1) return;
        var selectedGo = sel[0];
        if (selectedGo == null) return;
        var holder = selectedGo.GetComponent<MahjongTileHolder>();
        var chosenTile = holder != null ? holder.TileData : null;
        if (chosenTile == null) return;
        DeckMutationHelpers.AddCopiesToDeckFront(deckManager, chosenTile, 4);
        addConsumablePhase = 1;
        deckManager.selectedTiles.Clear();
        deckManager.sortHand();
    }

    public void ConfirmAddDiscard()
    {
        if (activeConsumable == null || deckManager == null || !IsAddType(activeConsumable) || addConsumablePhase != 1) return;
        var sel = deckManager.selectedTiles;
        if (sel == null || sel.Count != 4) return;
        deckManager.discardTiles(new List<GameObject>(sel));
        deckManager.drawHand(4);
        Finish();
    }

    private Button GetUseButton(int slotIndex)
    {
        if (slotIndex == 0) return useButtonSlot0;
        if (slotIndex == 1) return useButtonSlot1;
        return null;
    }

    private void Update()
    {
        if (activeConsumable == null) return;
        if (deckManager == null)
            deckManager = DeckManager.Instance ?? FindFirstObjectByType<DeckManager>();
        if (deckManager == null) return;
        var sel = deckManager.selectedTiles;
        var count = sel != null ? sel.Count : 0;

        if (IsAddType(activeConsumable))
        {
            if (copyButton != null)
            {
                bool showCopy = addConsumablePhase == 0;
                if (copyButton.gameObject.activeSelf != showCopy)
                    copyButton.gameObject.SetActive(showCopy);
                if (showCopy)
                    copyButton.interactable = count == 1;
            }
            var useBtn = GetUseButton(_slotIndexInUse);
            if (useBtn != null)
                useBtn.interactable = false;
        }
        else
        {
            if (copyButton != null && copyButton.gameObject.activeSelf)
                copyButton.gameObject.SetActive(false);

            // For non-Add consumables, required selection count may vary by code.
            int requiredCount = 1;
            if (activeConsumable != null)
            {
                var code = (activeConsumable.code ?? string.Empty).Trim().ToLowerInvariant();
                if (code == "totem")
                    requiredCount = 3;
            }

            bool selectionOk = count == requiredCount;
            var btn = GetUseButton(_slotIndexInUse);
            if (btn != null)
                btn.interactable = selectionOk;
        }
    }

    public void PurchaseFromShop()
    {
        if (shop == null) shop = FindFirstObjectByType<Shop>();
        if (shop == null || shop.consumableDrops == null || shop.consumableDrops.Count == 0) return;
        if (PlayerStatManager.Instance == null) return;
        PlayerStatManager.Instance.AddConsumableToInventory(shop.consumableDrops[0]);
    }


    private void Activate(Consumable consumable)
    {
        if (consumable == null) return;
        activeConsumable = consumable;

        if (deckManager == null)
            deckManager = DeckManager.Instance ?? FindFirstObjectByType<DeckManager>();

        if (deckManager != null && (deckManager.Hand == null || deckManager.Hand.Count == 0))
            deckManager.drawHand();

        // Econ-type consumables (e.g. Gold Coin): immediate cash gain, no tile selection.
        if (IsEquationType(consumable, "Econ"))
        {
            ApplyCash(3);
            Finish();
            return;
        }

        // Heal consumables (e.g. Zesty Orange, Golden Apple): immediate effect, no tile selection.
        if (IsEquationType(consumable, "Heal"))
        {
            var stats = PlayerStatManager.Instance;
            if (stats != null)
            {
                // CSV codes:
                // - "orange" => Zesty Orange: "Restores 1 Bar of Lost Health"
                // - "apple"  => Golden Apple: "Restores all Bars of Lost health"
                var code = (consumable.code ?? string.Empty).Trim().ToLowerInvariant();
                int amount = code == "orange" ? 1 : stats.maxHealth;
                stats.Heal(amount);

                if (StatsUpdater.Instance != null)
                    StatsUpdater.Instance.UpdateHealth(stats.currentHealth, stats.maxHealth);
            }

            Finish();
            return;
        }

        addConsumablePhase = 0;
        if (GameManager.Instance != null)
            GameManager.Instance.selecting = true;
    }

    private static bool IsEquationType(Consumable c, string type)
    {
        return c != null && string.Equals(c.equationType, type, System.StringComparison.OrdinalIgnoreCase);
    }

    // Add-type consumables: pick 1 tile -> add 4 to front of deck -> pick 4 to discard -> draw 4 (Clone Machine, etc.).
    private static bool IsAddType(Consumable c)
    {
        if (c == null || string.IsNullOrEmpty(c.equationType)) return false;
        var eq = c.equationType.Trim();
        return string.Equals(eq, "Add", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(eq, "Clone", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(eq, "Duplicate", System.StringComparison.OrdinalIgnoreCase);
    }

    // Use button click for a slot: if no active consumable, activate the one in that slot; if already active, treat as confirm and try to apply effect.
    private void UseSlot(int slotIndex)
    {
        var consumableAtSlot = ConsumableManager.Instance != null
            ? ConsumableManager.Instance.GetAt(slotIndex)
            : PlayerStatManager.Instance?.GetConsumableAt(slotIndex);

        if (activeConsumable == null)
        {
            if (consumableAtSlot != null)
            {
                _slotIndexInUse = slotIndex;
                Activate(consumableAtSlot);
            }
            return;
        }
        ConfirmTileSelection();
    }

    private void ConfirmTileSelection()
    {
        if (deckManager == null) return;
        var sel = deckManager.selectedTiles;
        if (sel == null) return;
        // Add/Clone flow uses Copy BTN (phase 0) and Discard button (phase 1), not Use.
        if (IsAddType(activeConsumable)) return;

        // Code-specific behaviors driven by CSV "Code" when equationType is Destroy/Enhance.
        var code = (activeConsumable.code ?? string.Empty).Trim().ToLowerInvariant();

        // Gun: "Choose a tile and Destroy every tile with a lower value from your hand".
        if (code == "gun")
        {
            if (sel.Count != 1) return;
            var go = sel[0];
            if (go == null) return;
            var h = go.GetComponent<MahjongTileHolder>();
            var chosen = h != null ? h.TileData : null;
            if (chosen == null) return;

            // Remove from hand any tile (any suit) with a strictly lower numbered value,
            // then refill the hand from the deck as normal.
            if (deckManager.Hand != null)
            {
                int chosenValue = (int)chosen.NumberedValue;
                if (chosenValue > 0)
                {
                    var handCopy = new List<GameObject>(deckManager.Hand);
                    foreach (var tileGo in handCopy)
                    {
                        if (tileGo == null) continue;
                        var th = tileGo.GetComponent<MahjongTileHolder>();
                        var data = th != null ? th.TileData : null;
                        if (data == null) continue;

                        int value = (int)data.NumberedValue;
                        if (value <= 0) continue;               // skip non-numbered tiles (winds/dragons/flowers/seasons)
                        if (value >= chosenValue) continue;     // strictly lower than chosen

                        deckManager.discardTile(tileGo);
                    }

                    // After destroying lower-value tiles, draw up to full hand as usual.
                    deckManager.redrawHand();
                }
            }

            Finish();
            return;
        }

        // Totem of Dying: "Select 3 tiles to permanently remove from the deck".
        if (code == "totem")
        {
            if (sel.Count != 3) return;
            var toRemove = new List<GameObject>(sel);
            foreach (var go in toRemove)
            {
                if (go == null) continue;
                var h = go.GetComponent<MahjongTileHolder>();
                var chosen = h != null ? h.TileData : null;
                if (chosen == null) continue;

                // Remove all future copies from the deck and discard the selected instance.
                DeckMutationHelpers.RemoveCopiesFromDeck(deckManager, chosen, int.MaxValue);
                deckManager.discardTile(go);
            }
            deckManager.redrawHand();
            Finish();
            return;
        }

        // Weighted Dice: "Choose a tile Suit and add 1 more of each tile in that suit to the deck".
        if (code == "dice")
        {
            if (sel.Count != 1) return;
            var go = sel[0];
            if (go == null) return;
            var h = go.GetComponent<MahjongTileHolder>();
            var chosen = h != null ? h.TileData : null;
            if (chosen == null) return;

            DeckMutationHelpers.AddSuitCopiesToDeck(deckManager, chosen.TileType);
            Finish();
            return;
        }

        // Generic non-Add consumables: single-tile selection (Destroy, Remove, Enhance).
        if (sel.Count != 1) return;
        {
            var go = sel[0];
            if (go == null) return;
            var h = go.GetComponent<MahjongTileHolder>();
            var chosen = h != null ? h.TileData : null;
            if (chosen == null) return;

            var eq = (activeConsumable.equationType ?? "").Trim();
            if (string.Equals(eq, "Destroy", System.StringComparison.OrdinalIgnoreCase))
                DeckMutationHelpers.RemoveCopiesFromDeck(deckManager, chosen, 4);
            else if (string.Equals(eq, "Remove", System.StringComparison.OrdinalIgnoreCase))
                DeckMutationHelpers.RemoveCopiesFromDeck(deckManager, chosen, int.MaxValue);
            else if (string.Equals(eq, "Enhance", System.StringComparison.OrdinalIgnoreCase))
                DeckMutationHelpers.EnhanceCopiesInDeckAndHand(deckManager, chosen, Edition.Ghost);
            else if (string.Equals(eq, "Heal", System.StringComparison.OrdinalIgnoreCase))
                ApplyHeal();

            Finish();
        }
    }

    private void ApplyHeal()
    {
        if (PlayerStatManager.Instance == null) return;
        PlayerStatManager.Instance.Heal(PlayerStatManager.Instance.maxHealth);
    }

    private void ApplyCash(int amount)
    {
        var stats = PlayerStatManager.Instance;
        if (stats == null) return;
        stats.cash += amount;

        if (StatsUpdater.Instance != null)
            StatsUpdater.Instance.UpdateCash(stats.cash);
    }

    private void Finish()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.selecting = true;

        if (deckManager != null && deckManager.selectedTiles != null)
        {
            deckManager.selectedTiles.Clear();
            deckManager.sortHand();
        }

        if (shopRoot != null)
            shopRoot.SetActive(true);
        if (copyButton != null)
            copyButton.gameObject.SetActive(false);

        int slotToRemove = _slotIndexInUse >= 0 ? _slotIndexInUse : (ConsumableManager.Instance != null ? ConsumableManager.Instance.SelectedIndex : -1);
        if (activeConsumable != null && slotToRemove >= 0)
        {
            if (ConsumableManager.Instance != null)
                ConsumableManager.Instance.RemoveAt(slotToRemove);
            else if (PlayerStatManager.Instance != null)
                PlayerStatManager.Instance.RemoveConsumableAt(slotToRemove);
        }

        activeConsumable = null;
        addConsumablePhase = 0;
        _slotIndexInUse = -1;
    }
}

// Helper methods for mutating the deck based on a tile "identity" (suit/value).
// Kept separate so the logic is reusable from other systems, not just consumables
public static class DeckMutationHelpers
{
    private static FieldInfo deckField;
    private static FieldInfo tileDataField;

    private static Deck GetDeck(DeckManager manager)
    {
        if (manager == null) return null;

        deckField ??= typeof(DeckManager).GetField("deck", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return deckField?.GetValue(manager) as Deck;
    }

    private static List<MahjongTileData> GetDeckTileList(DeckManager manager)
    {
        var deck = GetDeck(manager);
        if (deck == null) return null;

        tileDataField ??= typeof(Deck).GetField("tileData", BindingFlags.Instance | BindingFlags.NonPublic);
        return tileDataField?.GetValue(deck) as List<MahjongTileData>;
    }

    private static bool SameIdentity(MahjongTileData a, MahjongTileData b)
    {
        if (a == null || b == null) return false;
        return a.TileType == b.TileType
               && a.NumberedValue == b.NumberedValue
               && a.WindValue == b.WindValue
               && a.DragonValue == b.DragonValue
               && a.FlowerValue == b.FlowerValue
               && a.SeasonValue == b.SeasonValue;
    }

    private static MahjongTileData CloneWithEdition(MahjongTileData src, Edition? overrideEdition = null)
    {
        if (src == null) return null;
        var edition = overrideEdition ?? src.Edition;
        return new MahjongTileData(
            src.TileType,
            src.NumberedValue,
            src.WindValue,
            src.DragonValue,
            src.FlowerValue,
            src.SeasonValue,
            edition
        );
    }


    // Adds 'count' new copies of the specified tile identity to the deck (back of deck, drawn last).
    public static void AddCopiesToDeck(DeckManager manager, MahjongTileData identity, int count)
    {
        if (manager == null || identity == null || count <= 0) return;

        var deck = GetDeck(manager);
        if (deck == null) return;

        for (int i = 0; i < count; i++)
        {
            var clone = CloneWithEdition(identity);
            deck.AddTile(clone);
        }
    }

    // Adds one extra copy of every distinct tile identity of the given suit currently present in the deck.
    public static void AddSuitCopiesToDeck(DeckManager manager, TileType suit)
    {
        if (manager == null) return;

        var tiles = GetDeckTileList(manager);
        var deck = GetDeck(manager);
        if (tiles == null || deck == null) return;

        var seen = new HashSet<string>();
        foreach (var t in tiles)
        {
            if (t == null) continue;
            if (t.TileType != suit) continue;

            // Use GetTileString identity to avoid adding multiple copies per existing tile.
            string key = t.GetTileString();
            if (!seen.Add(key)) continue;

            var clone = CloneWithEdition(t);
            deck.AddTile(clone);
        }
    }

    // Adds 'count' new copies at the FRONT of the deck (drawn first). Use for Add consumable so discard-then-draw gives the new tiles.
    public static void AddCopiesToDeckFront(DeckManager manager, MahjongTileData identity, int count)
    {
        if (manager == null || identity == null || count <= 0) return;

        var deck = GetDeck(manager);
        if (deck == null) return;

        for (int i = 0; i < count; i++)
        {
            var clone = CloneWithEdition(identity);
            deck.AddTileAtFront(clone);
        }
    }


    // Removes up to maxCount copies of the specified tile identity from the deck.
    public static int RemoveCopiesFromDeck(DeckManager manager, MahjongTileData identity, int maxCount)
    {
        if (manager == null || identity == null || maxCount <= 0) return 0;

        var tiles = GetDeckTileList(manager);
        if (tiles == null) return 0;

        int removed = 0;
        for (int i = tiles.Count - 1; i >= 0 && removed < maxCount; i--)
        {
            if (!SameIdentity(tiles[i], identity)) continue;
            tiles.RemoveAt(i);
            removed++;
        }

        return removed;
    }


    // Sets the Edition of all matching tiles in the deck and current hand.
    public static void EnhanceCopiesInDeckAndHand(DeckManager manager, MahjongTileData identity, Edition newEdition)
    {
        if (manager == null || identity == null) return;

        var tiles = GetDeckTileList(manager);
        if (tiles != null)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!SameIdentity(tiles[i], identity)) continue;
                tiles[i] = CloneWithEdition(tiles[i], newEdition);
            }
        }

        // Also update any currently-instantiated tiles in the player's hand so visuals match.
        if (manager.Hand != null)
        {
            foreach (var go in manager.Hand)
            {
                if (go == null) continue;
                var holder = go.GetComponent<MahjongTileHolder>();
                if (holder == null || holder.TileData == null) continue;
                if (!SameIdentity(holder.TileData, identity)) continue;

                var updated = CloneWithEdition(holder.TileData, newEdition);
                holder.SetTileData(updated);
            }
        }
    }
}

