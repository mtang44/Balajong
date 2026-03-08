
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using TMPro;


public class Shop : MonoBehaviour
{
    [SerializeField]
    public GameObject[] Shop_Item_TMPs; // array of TMP objects that will display to shop items. 
    
    // [SerializeField]
    // public string[] Rarities = new string[] {"Common","Uncommon","Rare","Epic","Legendary"};
    // [SerializeField]
    // public int[] Rarity_Weights = new int[] {55,30,15,6,1};   
    public int lootCount = 5;
    private Shop myChest;

    void Start()
    {
        myChest = new Shop();
    }
    void Update()
    {
    }

    // call this function to open chest 
    public void RerollShop()
    {
        //some check for if player has enough currency to open chest here
        //myChest = LootChestGeneration(myChest);
        displayOutput(myChest);
    }
    
    public class ShopItems 
    {
        public string rarity;
        public string name;
        public string type;
        public int price;
        public string description;
        public ShopItems(string name, string rarity, string type, string description, int price = 0)
        {
            this.name = name;
            this.rarity = rarity;
            this.type = type;
            this.description = description;
            this.price = price;
        }
    }
    public List<ShopItems> drops = new List<ShopItems>();
    Dictionary <string, int> lootRarities = new  Dictionary<string, int>();
    Dictionary <string, List<ShopItems>> lootTable = new Dictionary<string, List<ShopItems>>();
    
   

    /* Returns a new chest instance with attached loot table paramaters. 
        If loot chest has not yet been generated, read from loot table csv and attach corresponding rarity weights.
       

        
    */
    public Shop LootChestGeneration(Shop chest)
    {
        chest.lootCount = lootCount;
        
        //TO DO
        //chest.lootTable = some function that reads from already generated Dictionary of Jokers

        // chest.lootRarities = some function that reads from already generateed Dictionary of rarities and weights
        // chest.drops = chest.GenerateLoot(chest.lootRarities, chest.lootCount);
        return chest;
    }
// Takes in a LootChest and displays it's generated loot to the Unity UI 
    public void displayOutput(Shop currentChest)
    {
        
        int dropIndex = 0;
        Debug.Log("Size of shop Item_TMPs:" + Shop_Item_TMPs.Length);
        foreach(GameObject shopSlot in Shop_Item_TMPs)
        {
            TMP_Text[] foundTMPs = shopSlot.GetComponentsInChildren<TMP_Text>(true);
            Debug.Log(shopSlot);
            foreach(TMP_Text currentTMP in foundTMPs)
            {
                if(currentTMP.name == "Price Tag TMP")
                {
                //     shopSlot.transform.Find("Price Tag TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].price; // set price tmp
                }
                else if(currentTMP.name == "Title TMP")
                {
                //     shopSlot.transform.Find("Title TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].name; // set Item Name tmp
                }
                else if(currentTMP.name == "Description TMP")
                {
                //     shopSlot.transform.Find("Description TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].description; // sets description of item to tmp
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
    public List<ShopItems> GenerateLoot(Dictionary<string, int> myRarity, int lootCount = 1 ) //
    {
        Dictionary <string, int> selectedRarity = new Dictionary<string, int>();
        List<ShopItems> selectedItems = new List<ShopItems>();
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
                ShopItems selectedItem = itemList[rand.Next(0,itemList.Count)];
                selectedItems.Add(selectedItem);
            }
        }   
        return selectedItems;
    }
}
