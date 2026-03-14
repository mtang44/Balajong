using System;
using UnityEngine;


// Coordinates consumable use flow: selection state and inventory read from PlayerStatManager.
// Place in RackingScene. Teammates hook UI to InventoryChanged / SelectionChanged and slot clicks call Select(index).
// Inventory is stored in PlayerStatManager (2 slots, persists entire game); this manager only holds selection.

public class ConsumableManager : MonoBehaviour
{
    public static ConsumableManager Instance { get; private set; }

    public const int InventorySize = 2;

    // Currently selected slot index (0 or 1), or -1 if none.
    public int SelectedIndex { get; private set; } = -1;

    public event Action InventoryChanged;
    public event Action<int> SelectionChanged;

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
        if (PlayerStatManager.Instance != null)
            PlayerStatManager.Instance.ConsumableInventoryChanged += OnPlayerStatsInventoryChanged;
    }

    private void OnDestroy()
    {
        if (PlayerStatManager.Instance != null)
            PlayerStatManager.Instance.ConsumableInventoryChanged -= OnPlayerStatsInventoryChanged;
        if (Instance == this)
            Instance = null;
    }

    private void OnPlayerStatsInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    // Select slot by index (0 or 1). Use -1 to clear selection. Only valid indices with a consumable are selectable.
    public void Select(int index)
    {
        if (index < -1 || index >= InventorySize) return;
        if (index >= 0 && GetAt(index) == null) return;
        SelectedIndex = index;
        SelectionChanged?.Invoke(SelectedIndex);
    }

    // Get the currently selected consumable, or null if none selected.
    public Consumable GetSelected()
    {
        return GetAt(SelectedIndex);
    }

    // Get consumable at slot (0 or 1). Returns null if empty or out of range.
    public Consumable GetAt(int index)
    {
        if (PlayerStatManager.Instance == null) return null;
        return PlayerStatManager.Instance.GetConsumableAt(index);
    }

    // Remove consumable at the given slot (e.g. after use). Call from ConsumableEffectSystem.
    public void RemoveConsumableAt(int index)
    {
        if (PlayerStatManager.Instance == null) return;
        PlayerStatManager.Instance.RemoveConsumableAt(index);
        if (SelectedIndex == index)
        {
            SelectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }
    }
}
