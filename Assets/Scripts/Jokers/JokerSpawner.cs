
using System.IO;
using System.Collections.Generic;
using UnityEngine;


public class JokerSpawner : MonoBehaviour
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
    Dictionary <string, List<Jokers>> lootTable = new Dictionary<string, List<Jokers>>();
    
   

    // at start of game read from joker csv file and creates a list of Joker Objects to use later for loot generation.   
    public void LootChestGeneration()
    {
        Debug.Log("Reading from csv file");
        lootTable = insertCustomLootTable(LootTableFilePath);
        foreach(var j in lootTable.Values)
        {
            foreach(var joker in j)
            {
                Debug.Log("Joker in loot table: " + joker.name);
            }
        }
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
    public Dictionary<string, List<Jokers>> insertCustomLootTable(string filePath)
    {
        string path = filePath;
        StreamReader reader;
        Dictionary<string, List<Jokers>> customLootTable = new Dictionary<string, List<Jokers>>();
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
                string jokerRarity = values[0];
                string jokerName = values[1];
                string jokerEquationType = values[2];
                string jokerDescription = values[3];
                int jokerPrice = int.Parse(values[4]);

                Jokers newJoker = new Jokers (jokerName, jokerRarity, jokerEquationType, jokerDescription, jokerPrice);

                // if item rarity key already exists, add newItem to the list, else create new rarity key with a new list of items. 
                if(customLootTable.ContainsKey(jokerRarity))
                {
                    customLootTable[jokerRarity].Add(newJoker);
                }
                else
                {
                    customLootTable.Add(jokerRarity, new List<Jokers> {newJoker});
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

    public Dictionary<string, List<Jokers>>  GetLootTable()
    {
        Debug.Log(lootTable);
        return lootTable;
    }
    public Dictionary<string,int> GetLootRarities()
    {
        
        return lootRarities;
    }
}