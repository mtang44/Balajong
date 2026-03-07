// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using Unity.VisualScripting;
using UnityEditorInternal;
using System.Xml;
using System.Collections;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEditor;

public class LootChest : MonoBehaviour
{
    [SerializeField]
    public string LootTableFilePath;
    [SerializeField]
    public GameObject[] Shop_Item_TMPs; // array of TMP objects that will display to shop items. 
    
    
    [SerializeField]  
    public int lootCount = 1;
    
    [SerializeField]
    public string[] Rarities = new string[] {"Common","Uncommon","Rare","Epic","Legendary"};
    [SerializeField]
    public int[] Rarity_Weights = new int[] {55,30,15,6,1};
   
    public string OutputPath;
    [SerializeField]
    public GameObject Chest_OutputUI;
    private LootChest myChest;

    void Start()
    {
        myChest = new LootChest();
    }
    void Update()
    {
    }

    // call this function to open chest 
    public void RerollShop()
    {
        //s ome check for if player has enough currency to open chest here
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
    public LootChest LootChestGeneration(LootChest chest)
    {
        chest.lootCount = lootCount;
        
        //TO DO
        //chest.lootTable = some function that reads from already generated Dictionary of Jokers

        //chest.lootRarities = some function that reads from already generateed Dictionary of rarities and weights
        //chest.drops = chest.GenerateLoot(chest.lootRarities, chest.lootCount);
        return chest;
    }
// Takes in a LootChest and displays it's generated loot to the Unity UI 
    public void displayOutput(LootChest currentChest)
    {
        
        int dropIndex = 0;
        Debug.Log("Size of shop Item_TMPs:" + Shop_Item_TMPs.Length);
        foreach(GameObject shopSlot in Shop_Item_TMPs)
        {
            TMP_Text[] foundTMPs = shopSlot.GetComponentsInChildren<TMP_Text>(true);
            Debug.Log(shopSlot);
            foreach(TMP_Text currentTMP in foundTMPs)
            {
                // if(currentTMP.name == "Price Tag TMP")
                // {
                //     shopSlot.transform.Find("Price Tag TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].price;; // set price tmp
                // }
                // else if(currentTMP.name == "Title TMP")
                // {
                //     shopSlot.transform.Find("Title TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].name; // set Item Name tmp
                // }
                // else if(currentTMP.name == "Description TMP")
                // {
                //     shopSlot.transform.Find("Description TMP").GetComponent<TMP_Text>().text = ""+ currentChest.drops[dropIndex].description; // sets description of item to tmp
                // }
            }
            // delete random color later
            shopSlot.GetComponentInChildren<RawImage>(true).color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)); // change later once we figure out joker images
            dropIndex++;
        }
    }
    // This function allows for custom rarities that correspond to the loot table .csv file's rarities, as well as the corresponding weights of each rarity. 
    public Dictionary<string, int> insertCustomRarities(string[] rarities, int[]weights)
    {
        Dictionary<string, int> customRarities = new Dictionary<string, int>();
        if(rarities.Length != weights.Length)
        {
            Debug.LogError("ERROR Rarity Categories and Rarity Weights of unequal size"); // error caused by inspector rarities and rarity weights being mismatched
            return customRarities;
        }
        for(int i = 0; i < rarities.Length; i++)
        {
            customRarities.Add(rarities[i],weights[i]);
        }
        return customRarities;
    }

    //Takes in .csv file path and parses it into a custom loot table dictionary
    // outputs a dictionary storing the string of the rarity category, and a list of objects that exist within that rarity
    public Dictionary<string, List<ShopItems>> insertCustomLootTable(string filePath)
    {
        string path = filePath;
        StreamReader reader;
        Dictionary<string, List<ShopItems>> customLootTable = new Dictionary<string, List<ShopItems>>();
        if(File.Exists(path))
        {
            reader = new StreamReader(File.OpenRead(path));
            if(!reader.EndOfStream)
            {
                reader.ReadLine(); // skip header line
            }
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                string itemRarity = values[0];
                string itemName = values[1];
                string itemType = values[2];
                string itemDescription = values[3];
                ShopItems newItem = new ShopItems (itemName, itemRarity, itemType, itemDescription);

                // if item rarity key already exists, add newItem to the list, else create new rarity key with a new list of items. 
                if(customLootTable.ContainsKey(itemRarity))
                {
                    customLootTable[itemRarity].Add(newItem);
                }
                else
                {
                    customLootTable.Add(itemRarity, new List<ShopItems> {newItem});
                }
                 
            }
            reader.Close();
            return customLootTable;
        }
        else
        {
            Debug.LogError("Error: File not found.");
            return customLootTable;
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

