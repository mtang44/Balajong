using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopPurchase : MonoBehaviour
{
    public GameObject ShopTextPanel;
    public GameObject ShopManager;
    public GameObject Player;
    private StatsUpdater statsUpdater;
    private void Start()
    {
        statsUpdater = GameObject.FindWithTag("StatsUpdater")?.GetComponent<StatsUpdater>();
    }
    public List<GameObject> jokerPanels = new List<GameObject>();
    public List<GameObject> consumablePanels = new List<GameObject>();

    public bool checkForCash(int cost)
    {
        if (PlayerStatManager.Instance.cash >= cost)
        {
            SoundManager.Instance.playCoinSound();
            PlayerStatManager.Instance.cash -= cost;
            PlayerStatManager.Instance.AddMoneySpent(cost);
            statsUpdater?.UpdateCash(PlayerStatManager.Instance.cash);

            Debug.Log("Purchase successful! Remaining cash: $" + PlayerStatManager.Instance.cash);
            StatsUpdater.Instance.UpdateCash(PlayerStatManager.Instance.cash);
            return true;
        }
        else
        {
            SoundManager.Instance.playIncorrectSound();
            Debug.Log("Not enough cash to make this purchase.");
        }
        return false;
    }
    public void rerollCheck()
    {
        TryPaidReroll(true, true);
    }

    public void rerollConsumablesCheck()
    {
        TryPaidReroll(false, true);
    }
    public void purchaseJoker(int index)
    {
        if(JokerManager.Instance.jokers.Count >= JokerManager.Instance.startingMaxJokers) return;
        if(checkForCash(ShopManager.GetComponent<Shop>().jokerDrops[index].price))
        {
            disableIndex(index);
            Jokers boughtJoker = ShopManager.GetComponent<Shop>().jokerDrops[index];
            Debug.Log("Added Joker code: " + ShopManager.GetComponent<Shop>().jokerDrops[index].code);
            string name = boughtJoker.name;
            string code = boughtJoker.code;
            string description = boughtJoker.description;
            int price =  boughtJoker.price;
            Texture img = ShopManager.GetComponent<Shop>().imageArray[boughtJoker.imageIndex];
            JokerManager.Instance.AddJoker(name, code, description,price, img);
        }

    }
    public void purchaseConsumable(int index)
    {
        Shop shop = ShopManager != null ? ShopManager.GetComponent<Shop>() : null;
        if (shop == null || shop.consumableDrops == null || index < 0 || index >= shop.consumableDrops.Count)
        {
            Debug.LogWarning("ShopPurchase: No consumable available at requested index.", this);
            return;
        }

        if (PlayerStatManager.Instance == null)
        {
            return;
        }

        if (!HasConsumableInventorySpace())
        {
            Debug.Log("Cannot buy consumable: inventory is full.");
            return;
        }

        Consumable boughtConsumable = shop.consumableDrops[index];
        if (!checkForCash(boughtConsumable.price))
        {
            return;
        }

        if (!PlayerStatManager.Instance.AddConsumableToInventory(boughtConsumable))
        {
            // Safety net if inventory changed between pre-check and add.
            PlayerStatManager.Instance.cash += boughtConsumable.price;
            statsUpdater?.UpdateCash(PlayerStatManager.Instance.cash);
            StatsUpdater.Instance?.UpdateCash(PlayerStatManager.Instance.cash);
            Debug.Log("Cannot buy consumable: inventory is full.");
            return;
        }

        DisableConsumableIndex(index, shop);
    }

    public void disableIndex(int index)
    {
        jokerPanels[index].SetActive(false);
    }

    private static bool HasConsumableInventorySpace()
    {
        if (PlayerStatManager.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < PlayerStatManager.ConsumableInventorySize; i++)
        {
            if (PlayerStatManager.Instance.GetConsumableAt(i) == null)
            {
                return true;
            }
        }

        return false;
    }

    private void DisableConsumableIndex(int index, Shop shop = null)
    {
        GameObject panel = ResolveConsumablePanel(index, shop);
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private GameObject ResolveConsumablePanel(int index, Shop shop = null)
    {
        if (index >= 0 && index < consumablePanels.Count && consumablePanels[index] != null)
        {
            return consumablePanels[index];
        }

        shop ??= ShopManager != null ? ShopManager.GetComponent<Shop>() : null;
        int shopSlotIndex = 4 + index;
        if (shop != null && shop.Shop_Item_TMPs != null && shopSlotIndex >= 0 && shopSlotIndex < shop.Shop_Item_TMPs.Length)
        {
            return shop.Shop_Item_TMPs[shopSlotIndex];
        }

        return null;
    }

    private void TryPaidReroll(bool rerollJokers, bool rerollConsumables)
    {
        int rerollCost = 5;
        if (!checkForCash(rerollCost))
        {
            return;
        }

        Shop shop = ShopManager != null ? ShopManager.GetComponent<Shop>() : null;
        if (shop == null)
        {
            Debug.LogWarning("ShopPurchase: ShopManager is missing or has no Shop component.", this);
            return;
        }

        if (rerollJokers)
        {
            foreach (GameObject panel in jokerPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(true);
                }
            }

            shop.RerollJokers();
        }

        if (rerollConsumables)
        {
            int visibleConsumableSlots = Mathf.Max(1, shop.consumableCount);
            for (int i = 0; i < visibleConsumableSlots; i++)
            {
                GameObject panel = ResolveConsumablePanel(i, shop);
                if (panel != null)
                {
                    panel.SetActive(true);
                }
            }

            shop.RerollConsumables();
        }
    }
}
