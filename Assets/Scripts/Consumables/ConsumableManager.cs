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

    // Cached consumable per slot when Select() was called, so Use button still gets the right consumable even if PlayerStatManager reference differs at click time.
    private readonly Consumable[] _cachedConsumableBySlot = new Consumable[2];

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
        {
            PlayerStatManager.Instance.ConsumableInventoryChanged += OnPlayerStatsInventoryChanged;
            RefreshCache();
        }
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
        RefreshCache();
        InventoryChanged?.Invoke();
    }

    public void Select(int index)
    {
        if (index < -1 || index >= InventorySize) return;
        if (index >= 0)
        {
            var atSlot = GetAt(index);
            if (atSlot == null) return;
            _cachedConsumableBySlot[index] = atSlot;
        }
        SelectedIndex = index;
        RefreshCache();
        SelectionChanged?.Invoke(SelectedIndex);
    }

    private void RefreshCache()
    {
        for (int i = 0; i < InventorySize; i++)
        {
            var c = GetAtInternal(i);
            if (c != null)
                _cachedConsumableBySlot[i] = c;
        }
    }

    private Consumable GetAtInternal(int index)
    {
        if (index < 0 || index >= InventorySize) return null;
        if (PlayerStatManager.Instance == null) return null;
        return PlayerStatManager.Instance.GetConsumableAt(index);
    }

    // Get consumable at slot (0 or 1). Returns null if empty or out of range. Uses cache if PM returns null so Use click still gets the consumable they selected.
    public Consumable GetAt(int index)
    {
        if (index < 0 || index >= InventorySize) return null;
        var fromStats = GetAtInternal(index);
        if (fromStats != null) return fromStats;
        return _cachedConsumableBySlot[index];
    }

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < InventorySize)
            _cachedConsumableBySlot[index] = null;
        if (PlayerStatManager.Instance == null) return;
        PlayerStatManager.Instance.RemoveConsumableAt(index);
        if (SelectedIndex == index)
        {
            SelectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }
    }
}
