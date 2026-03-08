// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   
using TMPro;

public class LootChest : MonoBehaviour
{
    [SerializeField]
    public string LootTableFilePath;
    
    [SerializeField]  
    public int BatchGenerationCount = 1;
    [SerializeField]  
    public int lootCount = 1;
    [SerializeField]
    public bool allowChestRegeneration = false; public bool OutputToFile;
    [SerializeField]
    public string[] Rarities = new string[] {"Common","Uncommon","Rare","Epic","Legendary"};
    [SerializeField]
    public int[] Rarity_Weights = new int[] {55,30,15,6,1};
    [SerializeField]
    public Button GenerateBTN;
    [SerializeField]
    public TextMeshProUGUI Chest_Output_TMP;
    [SerializeField]
    public string OutputPath;
    [SerializeField]
    public GameObject Chest_OutputUI;
    public bool inChest = false;
    private bool alreadyGenerated;
    private LootChest myChest;

    void Start()
    {
        Button btn = GenerateBTN.GetComponent<Button>();
        myChest = new LootChest();
		btn.onClick.AddListener(Open);
        Chest_OutputUI.SetActive(false);
    }
    void Update()
    {
        
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     if(!inChest)
        //     {
        //         Open();
        //         inChest = true;
        //     }
        //     else
        //     {
        //         inChest = false;
        //         closeOutputWindow();
        //     }
        // }
    }

    // call this function to open chest 
    public void Open(){

        // generates a new chest with loot if 1: chest has not been opened 2: chest allows for regeneration of loot upon open
        // else display existing chest's already generated loot 

        if(!alreadyGenerated || allowChestRegeneration)
        {
            Debug.Log("New chest created");
            myChest = LootChestGeneration(myChest);
           
            if(!alreadyGenerated)
            {
                GetComponent<Animation>().Play();
            }
            alreadyGenerated = true;
           
            displayOutput(myChest);
        }
        else
        {
            Debug.Log("Chest already Exists");
            displayOutput(myChest);
        }
        
	}
    // c# object that stores data about object created in csv file
    public class gameItem 
    {
        public string rarity;
        public string name;
        // public int quantity;
        public string type;
        public string description;
        public gameItem(string name, string rarity, string type, string description)
        {
            this.name = name;
            this.rarity = rarity;
            this.type = type;
            this.description = description;
        }
    }
    public List<gameItem> drops = new List<gameItem>();
    Dictionary <string, int> lootRarities = new  Dictionary<string, int>();
    Dictionary <string, List<gameItem>> lootTable = new Dictionary<string, List<gameItem>>();
    
   

    /* Returns a new chest instance with attached loot table paramaters. 
        If loot chest has not yet been generated, read from loot table csv and attach corresponding rarity weights.
       

        
    */
    public LootChest LootChestGeneration(LootChest chest)
    {
        List<List<gameItem>> batchDrops = new List<List<gameItem>>();
        if(!alreadyGenerated) //  This prevents a lootchest from having to reread the csv file every time it generates new loot
        {
            chest.lootCount = lootCount;
            chest.lootTable = chest.insertCustomLootTable(LootTableFilePath);
            chest.lootRarities = chest.insertCustomRarities(Rarities, Rarity_Weights); 
        }
        for(int i = 0; i < BatchGenerationCount; i++)
        {
            chest.drops = chest.GenerateLoot(chest.lootRarities, chest.lootCount);
            if(OutputToFile)
            {
                batchDrops.Add(chest.drops);
            }
        }
        if(OutputToFile)
        {
            outputToFile(OutputPath, batchDrops);
        }
        return chest;
    }
    // Takes in a list of chests outputs, where each chest output is stored as a List of gameItems and overwrites them into filePath
    // List<List<gameItems> allows for batch generation output 
    public void outputToFile(string filePath, List<List<gameItem>> batchDrops)
    {
        StreamWriter writer;
        int count = 1;
        if(File.Exists(filePath))
        {
            writer = new StreamWriter(filePath, false);
            foreach(var chestDrop in batchDrops)
            {
                writer.Write("Chest " + count + ": {");

                   for (int i = 0; i < chestDrop.Count; i++)
                    {
                    writer.Write(chestDrop[i].name);

                    if (i < chestDrop.Count - 1)
                    {
                        writer.Write(", ");
                    }
                            
                    }
                writer.Write("}");
                writer.WriteLine();
                count++;
            }
            writer.Close();
        } 
        else
        {
            Debug.LogError("Error: Output File not found.");
        }

    }
        // Takes in a LootChest and displays it's generated loot to the Unity UI 
    public void displayOutput(LootChest currentChest)
    {
        string output = "";
        Chest_OutputUI.SetActive(true);
        foreach(var drop in currentChest.drops)
        {
            output += "1 x ("  + drop.rarity + ") " +  drop.name + "\n" ;
        }   
        Chest_Output_TMP.text = output; 
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
    public Dictionary<string, List<gameItem>> insertCustomLootTable(string filePath)
    {
        string path = filePath;
        StreamReader reader;
        Dictionary<string, List<gameItem>> customLootTable = new Dictionary<string, List<gameItem>>();
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
                gameItem newItem = new gameItem (itemName, itemRarity, itemType, itemDescription);

                // if item rarity key already exists, add newItem to the list, else create new rarity key with a new list of items. 
                if(customLootTable.ContainsKey(itemRarity))
                {
                    customLootTable[itemRarity].Add(newItem);
                }
                else
                {
                    customLootTable.Add(itemRarity, new List<gameItem> {newItem});
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
    public List<gameItem> GenerateLoot(Dictionary<string, int> myRarity, int lootCount = 1 ) //
    {
        Dictionary <string, int> selectedRarity = new Dictionary<string, int>();
        List<gameItem> selectedItems = new List<gameItem>();
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
        // Step 6: 
        foreach(var r in selectedRarity)
        {
            for(int i = 0; i < r.Value; i++)
            {
                var itemList = lootTable[r.Key]; 
                gameItem selectedItem = itemList[rand.Next(0,itemList.Count)];
                selectedItems.Add(selectedItem);
            }
        }   
        return selectedItems;
    }

    // closes the Chest's UI output Window
    public void closeOutputWindow()
    {
        
        Chest_OutputUI.SetActive(false);
        
    }
}

