using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Handles the immediate-activation flow for shop consumables.
// - When a consumable is purchased, it hides the shop, ensures a hand is drawn,
//   and exposes a single "Use Consumable" button.
// - The button only works when exactly one tile is selected in the hand.
// - Effects are implemented in a reusable, deck-agnostic way via DeckMutationHelpers
public class ConsumableEffectSystem : MonoBehaviour
{
    public static ConsumableEffectSystem Instance { get; private set; }

    [Header("Scene References (optional; auto-found if null)")]
    [SerializeField] private Shop shop;
    [SerializeField] private GameObject shopRoot;   // Root panel to hide while using a consumable
    [SerializeField] private Button useButton;      // "Use Consumable" button

    private DeckManager deckManager;
    private Consumable activeConsumable;
    // Add consumable is two-phase: 0 = select tile type (1 tile), 1 = select 4 to discard
    private int addConsumablePhase;

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

        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
            useButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (useButton == null || deckManager == null) return;

        var hasActive = activeConsumable != null;
        var sel = deckManager.selectedTiles;
        var count = sel != null ? sel.Count : 0;
        bool buttonOk = false;
        if (hasActive)
        {
            if (activeConsumable.equationType == "Add")
                buttonOk = (addConsumablePhase == 0 && count == 1) || (addConsumablePhase == 1 && count == 4);
            else
                buttonOk = count == 1;
        }
        useButton.interactable = buttonOk;
    }


    // Entry point for the shop "buy" button: purchase and immediately activate the current shop consumable.
    // Wire this from the voucher/consumable slot button's OnClick.
    public void PurchaseAndActivateCurrentShopConsumable()
    {
        if (shop == null) shop = FindFirstObjectByType<Shop>();
        if (shop == null || shop.consumableDrops == null || shop.consumableDrops.Count == 0)
        {
            Debug.LogWarning("[ConsumableEffectSystem] No consumable available in shop.");
            return;
        }

        ActivateConsumable(shop.consumableDrops[0]);
    }


    // Starts the immediate-use flow for a given consumable.
    public void ActivateConsumable(Consumable consumable)
    {
        if (consumable == null) return;

        activeConsumable = consumable;

        // Hide shop UI while resolving the consumable.
        if (shopRoot != null)
            shopRoot.SetActive(false);

        if (deckManager == null)
            deckManager = DeckManager.Instance ?? FindFirstObjectByType<DeckManager>();

        // Ensure a hand is drawn so the player can choose a tile.
        if (deckManager != null)
        {
            var hand = deckManager.Hand;
            if (hand == null || hand.Count == 0)
            {
                deckManager.drawHand();
            }
        }
        // If this is a Heal consumable, we can immediately apply it without a tile choice.
        if (consumable.equationType == "Heal")
        {
            ApplyHeal();
            FinishConsumable();
            return;
        }

        // Otherwise we need a tile selection; show the Use button and wait for selection (1 tile for type, or 4 for Add discard phase).
        addConsumablePhase = 0;
        if (GameManager.Instance != null)
            GameManager.Instance.selecting = true;
        if (useButton != null)
        {
            useButton.gameObject.SetActive(true);
        }
    }

    private void OnUseButtonClicked()
    {
        Debug.Log("Use Button Clicked");
        if (activeConsumable == null || deckManager == null) return;
        var sel = deckManager.selectedTiles;
        if (sel == null) return;

        if (activeConsumable.equationType == "Add")
        {
            if (addConsumablePhase == 0)
            {
                if (sel.Count != 1) return;
                var selectedGo = sel[0];
                if (selectedGo == null) return;
                var holder = selectedGo.GetComponent<MahjongTileHolder>();
                var chosenTile = holder != null ? holder.TileData : null;
                if (chosenTile == null) return;

                // Add 4 copies to FRONT of deck so they are drawn next.
                DeckMutationHelpers.AddCopiesToDeckFront(deckManager, chosenTile, 4);
                addConsumablePhase = 1;
                deckManager.selectedTiles.Clear();
                deckManager.sortHand();
                return;
            }

            if (addConsumablePhase == 1)
            {
                if (sel.Count != 4) return;
                // Discard the 4 selected tiles, then draw 4 from deck (the 4 we added at front).
                deckManager.discardTiles(new List<GameObject>(sel));
                deckManager.drawHand(4);
                FinishConsumable();
                return;
            }
        }

        // Non-Add consumables, or single-tile selection (Destroy, Enhance)
        if (sel.Count != 1) return;
        var go = sel[0];
        if (go == null) return;
        var h = go.GetComponent<MahjongTileHolder>();
        var chosen = h != null ? h.TileData : null;
        if (chosen == null) return;

        switch (activeConsumable.equationType)
        {
            case "Destroy":
                DeckMutationHelpers.RemoveCopiesFromDeck(deckManager, chosen, 4);
                break;
            case "Enhance":
                DeckMutationHelpers.EnhanceCopiesInDeckAndHand(deckManager, chosen, Edition.Ghost);
                break;
            case "Heal":
                ApplyHeal();
                break;
            default:
                Debug.LogWarning($"[ConsumableEffectSystem] Unknown equationType '{activeConsumable.equationType}'.");
                break;
        }

        FinishConsumable();
    }

    private void ApplyHeal()
    {
        var stats = PlayerStatManager.Instance;
        if (stats == null)
        {
            Debug.LogWarning("[ConsumableEffectSystem] Heal consumable used but PlayerStatManager.Instance was null.");
            return;
        }

        // Healing by maxHealth will always clamp to full because Heal() enforces maxHealth.
        stats.Heal(stats.maxHealth);
    }

    private void FinishConsumable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.selecting = false;

        // Clear selection state and hide use button.
        if (deckManager != null && deckManager.selectedTiles != null)
        {
            deckManager.selectedTiles.Clear();
            deckManager.sortHand();
        }

        if (useButton != null)
        {
            useButton.gameObject.SetActive(false);
        }

        // Show shop again.
        if (shopRoot != null)
        {
            shopRoot.SetActive(true);
        }

        activeConsumable = null;
        addConsumablePhase = 0;
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
        if (deck == null)
        {
            Debug.LogWarning("[DeckMutationHelpers] Could not access deck to add copies.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var clone = CloneWithEdition(identity);
            deck.AddTile(clone);
        }
    }

    // Adds 'count' new copies at the FRONT of the deck (drawn first). Use for Add consumable so discard-then-draw gives the new tiles.
    public static void AddCopiesToDeckFront(DeckManager manager, MahjongTileData identity, int count)
    {
        if (manager == null || identity == null || count <= 0) return;

        var deck = GetDeck(manager);
        if (deck == null)
        {
            Debug.LogWarning("[DeckMutationHelpers] Could not access deck to add copies at front.");
            return;
        }

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
        if (tiles == null)
        {
            Debug.LogWarning("[DeckMutationHelpers] Could not access deck tile list to remove copies.");
            return 0;
        }

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

