using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ShopPurchase : MonoBehaviour
{
    public GameObject ShopTextPanel;
    public GameObject ShopManager;
    public GameObject Player;
 
    public bool checkForCash(int cost)
    {
        if (PlayerStatManager.Instance.cash >= cost)
        {
            PlayerStatManager.Instance.cash -= cost;
            Debug.Log("Purchase successful! Remaining cash: $" + PlayerStatManager.Instance.cash);
            ShopTextPanel.GetComponent<UpdateCashOnEnable>().Start();
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
            ShopManager.GetComponent<Shop>().RerollJokers();
        }
    }
    public void purchaseJoker(int index)
    {
        if(checkForCash(ShopManager.GetComponent<Shop>().jokerDrops[index].price))
        {
            Debug.Log("Added Joker code: " + ShopManager.GetComponent<Shop>().jokerDrops[index].code);
            JokerManager.Instance.jokers.Add(ShopManager.GetComponent<Shop>().jokerDrops[index].code);
        }

    }
}
