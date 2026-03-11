
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using TMPro;


public class Shop : MonoBehaviour
{
    

    [SerializeField]
    public GameObject[] Shop_Item_TMPs; // array of TMP objects that will display to shop items. 
    public JokerSpawner jokerSpawner;
    public int jokerCount = 2;

    public List<Jokers> drops = new List<Jokers>();
    Dictionary <string, int> lootRarities = new  Dictionary<string, int>();
    Dictionary <string, List<Jokers>> lootTable = new Dictionary<string, List<Jokers>>();
    void Start()
    {
        RerollShop();
    }
    void Update()
    {
    }
    public void RerollShop()
    {
        //some check for if player has enough currency to open chest here
        LootChestGeneration();
        displayOutput();
    }
   
    
    
    public void LootChestGeneration()
    {   
        lootTable = jokerSpawner.GetLootTable();
        lootRarities = jokerSpawner.GetLootRarities();
        drops = GenerateLoot(lootRarities, jokerCount);
    }
// Takes in a LootChest and displays it's generated loot to the Unity UI 
    public void displayOutput()
    {   
        int dropIndex = 0;
        Debug.Log("Size of shop Item_TMPs:" + Shop_Item_TMPs.Length);
        for(int i = 0; i < 2; i++) 
        {
            GameObject shopSlot = Shop_Item_TMPs[i];
            TMP_Text[] foundTMPs = shopSlot.GetComponentsInChildren<TMP_Text>(true);
            foreach(TMP_Text currentTMP in foundTMPs)
            {
                if(currentTMP.name == "Price Tag TMP")
                {
                    currentTMP.transform.GetComponent<TMP_Text>().text = ""+ drops[dropIndex].price; // set price tmp
                }
                else if(currentTMP.name == "Title TMP")
                {
                    currentTMP.transform.GetComponent<TMP_Text>().text  = ""+ drops[dropIndex].name; // set Item Name tmp
                }
                else if(currentTMP.name == "Description TMP")
                {
                    currentTMP.transform.GetComponent<TMP_Text>().text  = ""+ drops[dropIndex].description; // sets description of item to tmp
                }
            }
            // delete random color later
            shopSlot.GetComponentInChildren<RawImage>(true).color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)); // change later once we figure out joker images
            dropIndex++;
        }
    }
    
    /* This is the main function that generates the loot from the loot table. 
      Takes in a Dictionary of custom rarities and their corresponding weights, and an integer for how many items to generate from the loot table, 
      outputs a list of gameItems corresponding to items in loot table
     
        Steps: 
            1: Initialize selectedRarity dictionary and calculate weighted sum of rarities
            2: Using weighted sum, generate a random number beftween 0 and sum.  
            3: take previous generated number and subtract it from the next higest rarity. If the remaining number is >= next rarity repeat with next lowest rarity. 
            4: If the number < next lowest rarity, then that is the selected rarity. 
            5: When a rarity is selected add it to the output dictionary. If already existing increment it's counter. 
            6: With the selected rarities, randomly select items from the loot table based on the number of items per rarity. 
    */
    public List<Jokers> GenerateLoot(Dictionary<string, int> myRarity, int lootCount = 1 ) //
    {
        Dictionary <string, int> selectedRarity = new Dictionary<string, int>();
        List<Jokers> selectedItems = new List<Jokers>();
        System.Random rand = new System.Random();
        int weightedSum = 0;

        //Step 1
        foreach(var rarity in myRarity)
        {
            selectedRarity.Add(rarity.Key, 0);
            weightedSum += rarity.Value;
        }
        if(weightedSum <= 0) 
        {
            Debug.Log("Error: No loot available.");
            return selectedItems;
        }

        for(int i = 0; i < lootCount; i++) // number of items to gererate
        {
            //Step 2: 
            int roll = rand.Next(0,weightedSum); 
            //Step 3: 
            foreach(var rarity in myRarity)
            {
                roll -= rarity.Value;
                if(roll < 0)
                {
                    //Step 5
                    if(selectedRarity.ContainsKey(rarity.Key))
                    {
                        selectedRarity[rarity.Key] += 1;
                    }
                    else
                    {
                        selectedRarity.Add(rarity.Key,1);
                    }
                }
            }
        }
        // Step 6: picking item from rarity // NEED FOR SURE
        foreach(var r in selectedRarity)
        {
            for(int i = 0; i < r.Value; i++)
            {
                var itemList = lootTable[r.Key]; 
                Jokers selectedItem = itemList[rand.Next(0,itemList.Count)];
                selectedItems.Add(selectedItem);
            }
        }   
        return selectedItems;
    }
}
