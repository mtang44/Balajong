
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using TMPro;


public class Shop : MonoBehaviour
{
    

    [SerializeField]
    public GameObject[] Shop_Item_TMPs; // array of TMP objects that will display to shop items. 
    public JokerSpawner jokerSpawner;
    [SerializeField]
    public Texture[] imageArray;
    public ConsumableGenerator consumableGenerator;
    public int jokerCount = 2;
    public int consumableCount = 1; 

    public List<Jokers> jokerDrops = new List<Jokers>();
    public List<Consumable> consumableDrops = new List<Consumable>();
    Dictionary <string, int> jokerlootRarities = new  Dictionary<string, int>();
    Dictionary <string, List<Jokers>> jokerLootTable = new Dictionary<string, List<Jokers>>();
    Dictionary <string, List<Consumable>> consumableLootTable = new Dictionary<string, List<Consumable>>();
    Dictionary <string, int> consumablelootRarities = new  Dictionary<string, int>();
    void Start()
    {
        RerollJokers();
        RerollConsumables();
        // reroll tiles here later
    }
    void Update()
    {
    }
    public void RerollConsumables()
    {
        consumableLootTable = consumableGenerator.GetLootTable();
        consumablelootRarities = consumableGenerator.GetLootRarities();
        consumableDrops = GenerateLoot<Consumable>(consumablelootRarities, consumableLootTable, consumableCount);
        displayConsumableOutput(consumableDrops);
    }
    public void RerollJokers()
    {
        //some check for if player has enough currency to open chest here
        jokerLootTable = jokerSpawner.GetLootTable();
        jokerlootRarities = jokerSpawner.GetLootRarities();
        jokerDrops = GenerateLoot<Jokers>(jokerlootRarities,jokerLootTable, jokerCount);
        displayJokerOutput(jokerDrops);
    }
   
// Takes in a LootChest and displays it's generated loot to the Unity UI 
    public void displayJokerOutput(List<Jokers> drops = null)
    {   
        for(int dropIndex = 0; dropIndex < 2; dropIndex++) 
        {
            GameObject shopSlot = Shop_Item_TMPs[dropIndex];
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
             shopSlot.GetComponentInChildren<RawImage>(true).texture = imageArray[drops[dropIndex].imageIndex];
        }
    }
    public void displayConsumableOutput(List<Consumable> drops = null)
    {   
       
        GameObject shopSlot = Shop_Item_TMPs[4];
        TMP_Text[] foundTMPs = shopSlot.GetComponentsInChildren<TMP_Text>(true);
        foreach(TMP_Text currentTMP in foundTMPs)
        {
            if(currentTMP.name == "Price Tag TMP")
            {
                currentTMP.transform.GetComponent<TMP_Text>().text = ""+ drops[0].price; // set price tmp
            }
            else if(currentTMP.name == "Title TMP")
            {
                currentTMP.transform.GetComponent<TMP_Text>().text  = ""+ drops[0].name; // set Item Name tmp
            }
            else if(currentTMP.name == "Description TMP")
            {
                currentTMP.transform.GetComponent<TMP_Text>().text  = ""+ drops[0].description; // sets description of item to tmp
            }
        }
        // delete random color later
        shopSlot.GetComponentInChildren<RawImage>(true).color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)); // change later once we figure out joker images
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
    public List<T> GenerateLoot<T>(Dictionary<string, int> myRarity, Dictionary<string,List<T>>lootTable, int lootCount = 1) //
    {
        Dictionary <string, int> selectedRarity = new Dictionary<string, int>();
        List<T> selectedItems = new List<T>();
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
                    break;
                }
            }
        }
        // Step 6: picking item from rarity // NEED FOR SURE
        foreach(var r in selectedRarity)
        {
            for(int i = 0; i < r.Value; i++)
            {
                var itemList = lootTable[r.Key]; 
                T selectedItem = itemList[rand.Next(0,itemList.Count)];
                selectedItems.Add(selectedItem);
            }
        }   
        return selectedItems;
    }
}
