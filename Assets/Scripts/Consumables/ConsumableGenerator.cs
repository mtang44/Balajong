
using System.IO;
using System.Collections.Generic;
using UnityEngine;


public class ConsumableGenerator : MonoBehaviour
{
    [SerializeField]
    public string LootTableFilePath;

    public string[] Rarities = new string[] {"Common","Uncommon","Rare","Epic","Legendary"};
    [SerializeField]
    public int[] Rarity_Weights = new int[] {55,30,15,6,1};

    void Awake()
    {  
        Debug.Log("awake called in joker spawner");
       LootChestGeneration();
    }
    void Update()
    {
    }

    // call this function to open chest 
    // public void Open(){
     
	// }
    // c# object that stores data about object created in csv file
    
    Dictionary <string, int> lootRarities = new  Dictionary<string, int>();
    Dictionary <string, List<Consumable>> lootTable = new Dictionary<string, List<Consumable>>();
    
   

    // at start of game read from joker csv file and creates a list of Joker Objects to use later for loot generation.   
    public void LootChestGeneration()
    {
        Debug.Log("Reading from csv file");
        lootTable = insertCustomLootTable(LootTableFilePath);
        lootRarities = insertCustomRarities(Rarities, Rarity_Weights); 
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
    public Dictionary<string, List<Consumable>> insertCustomLootTable(string filePath)
    {
        string path = filePath;
        StreamReader reader;
        Dictionary<string, List<Consumable>> customLootTable = new Dictionary<string, List<Consumable>>();
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
                string consumableRarity = values[0];
                string consumableName = values[1];
                string consumableCode = values[2];
                string consumableEquationType = values[3];
                string consumableDescription = values[4];
                int consumablePrice = int.Parse(values[5]);

                Consumable newConsumableItem = new Consumable (consumableName, consumableRarity, consumableCode, consumableEquationType, consumableDescription, consumablePrice);

                // if item rarity key already exists, add newItem to the list, else create new rarity key with a new list of items. 
                if(customLootTable.ContainsKey(consumableRarity))
                {
                    customLootTable[consumableRarity].Add(newConsumableItem);
                }
                else
                {
                    customLootTable.Add(consumableRarity, new List<Consumable> {newConsumableItem});
                }
                //Debug.Log("Joker added to loot table: " + newJoker.name + " of rarity " + newJoker.rarity);
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

    public Dictionary<string, List<Consumable>>  GetLootTable()
    {
        Debug.Log(lootTable);
        return lootTable;
    }
    public Dictionary<string,int> GetLootRarities()
    {
        
        return lootRarities;
    }
}