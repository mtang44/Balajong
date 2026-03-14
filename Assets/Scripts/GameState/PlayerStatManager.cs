using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    public static PlayerStatManager Instance;

    private int startingMaxHealth;
    private int startingCash;

    [Header("Health")]
    public int maxHealth = 4;
    public int currentHealth;
    public int cash = 0;

    [Header("Consumable inventory (entire game, limit 2)")]
    public const int ConsumableInventorySize = 2;
    private readonly List<Consumable> _consumableSlots = new List<Consumable> { null, null };

    public event Action ConsumableInventoryChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            startingMaxHealth = maxHealth;
            startingCash = cash;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void ResetRunState()
    {
        maxHealth = startingMaxHealth;
        currentHealth = maxHealth;
        cash = startingCash;

        for (int i = 0; i < _consumableSlots.Count; i++)
        {
            _consumableSlots[i] = null;
        }

        ConsumableInventoryChanged?.Invoke();
    }

    // Get consumable at slot (0 or 1). Returns null if slot empty or out of range.
    public Consumable GetConsumableAt(int index)
    {
        if (index < 0 || index >= ConsumableInventorySize) return null;
        return _consumableSlots[index];
    }

    // Add a consumable to the first empty slot. Returns true if added, false if inventory full.
    public bool AddConsumableToInventory(Consumable consumable)
    {
        if (consumable == null) return false;
        for (int i = 0; i < ConsumableInventorySize; i++)
        {
            if (_consumableSlots[i] == null)
            {
                _consumableSlots[i] = new Consumable(consumable);
                ConsumableInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // Remove consumable at slot (sets to null). Call when item is used.
    public void RemoveConsumableAt(int index)
    {
        if (index < 0 || index >= ConsumableInventorySize) return;
        _consumableSlots[index] = null;
        ConsumableInventoryChanged?.Invoke();
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
        }
    }
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    public void setNewMaxHealth(int newMaxHealth, bool updateCurrentHealth = true)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth || updateCurrentHealth)
        {
            currentHealth = maxHealth;
        }
    }
    public void updateTheMax()
    {
        maxHealth = 4 + (3 * JokerManager.Instance.numberOfActivations("medic"));
    }
}


