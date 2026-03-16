using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopPurchase : MonoBehaviour
{
    public GameObject ShopTextPanel;
    public GameObject ShopManager;
    public GameObject Player;
    public List<GameObject> jokerPanels = new List<GameObject>();
 
    public bool checkForCash(int cost)
    {
        if (PlayerStatManager.Instance.cash >= cost)
        {
            PlayerStatManager.Instance.cash -= cost;
            Debug.Log("Purchase successful! Remaining cash: $" + PlayerStatManager.Instance.cash);
            StatsUpdater.Instance.UpdateCash(PlayerStatManager.Instance.cash);
            return true;
        }
        else
        {
            Debug.Log("Not enough cash to make this purchase.");
        }
        return false;
    }
    public void rerollCheck()
    {
        int rerollCost = 5;
        if(checkForCash(rerollCost))
        {
            foreach(GameObject panel in jokerPanels)
            {
                panel.SetActive(true);
            }
            ShopManager.GetComponent<Shop>().RerollJokers();
        }
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
    public void disableIndex(int index)
    {
        jokerPanels[index].SetActive(false);
    }
}
