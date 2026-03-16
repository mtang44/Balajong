
using System.IO;
using System.Collections.Generic;
using UnityEngine;


public class ConsumableGenerator : MonoBehaviour
{
    [SerializeField] private string LootTableFilePath;
    [SerializeField] private string[] Rarities = new string[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
    [SerializeField] private int[] Rarity_Weights = new int[] { 55, 30, 15, 6, 1 };

    private void Awake()
    {
        LoadLoot();
    }

    // Loot table: rarity -> list of consumables. Rarities and weights for roll.
    
    private Dictionary<string, int> lootRarities = new Dictionary<string, int>();
    private Dictionary<string, List<Consumable>> lootTable = new Dictionary<string, List<Consumable>>();
    
   

    // at start of game read from joker csv file and creates a list of Joker Objects to use later for loot generation.   
    private void LoadLoot()
    {
        lootTable = LoadLootTable(LootTableFilePath);
        lootRarities = LoadRarities(Rarities, Rarity_Weights);
    }
    private Dictionary<string, int> LoadRarities(string[] rarities, int[] weights)
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

    private Dictionary<string, List<Consumable>> LoadLootTable(string filePath)
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
            int imageIndex = 0;
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
                

                Consumable newConsumableItem = new Consumable (consumableName, consumableRarity, consumableCode, consumableEquationType, consumableDescription, consumablePrice,imageIndex);

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
                imageIndex++;
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

    public Dictionary<string, List<Consumable>> GetLootTable()
    {
        return lootTable;
    }
    public Dictionary<string,int> GetLootRarities()
    {
        
        return lootRarities;
    }
}