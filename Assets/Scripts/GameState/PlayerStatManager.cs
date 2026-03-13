using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    //Welcome to the global holder of the player's most important stat: health!
    public static PlayerStatManager Instance;
    public int maxHealth = 4;
    public int currentHealth;
    public int cash = 0;

    void Awake()
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
    void Start()
    {
        currentHealth = maxHealth;
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
}
