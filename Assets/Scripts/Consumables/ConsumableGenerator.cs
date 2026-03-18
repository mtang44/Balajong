
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;


public class ConsumableGenerator : MonoBehaviour
{
    [SerializeField] private string LootTableFilePath;
    [SerializeField] private TextAsset lootTableTextAsset;
    [SerializeField] private string lootTableResourcePath;
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
        Dictionary<string, List<Consumable>> customLootTable = new Dictionary<string, List<Consumable>>();

        if (!TryReadLootLines(filePath, out List<string> lines))
        {
            return customLootTable;
        }

        int imageIndex = 0;
        for (int lineIndex = 1; lineIndex < lines.Count; lineIndex++)
        {
            string line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] values = line.Split(',');
            if (values.Length < 6)
            {
                Debug.LogWarning($"ConsumableGenerator: Skipping malformed loot line {lineIndex + 1}.", this);
                continue;
            }

            string consumableRarity = values[0].Trim();
            string consumableName = values[1].Trim();
            string consumableCode = values[2].Trim();
            string consumableEquationType = values[3].Trim();
            string consumableDescription = values[4].Trim();
            if (!int.TryParse(values[5], out int consumablePrice))
            {
                Debug.LogWarning($"ConsumableGenerator: Invalid price on loot line {lineIndex + 1}.", this);
                continue;
            }

            Consumable newConsumableItem = new Consumable(consumableName, consumableRarity, consumableCode, consumableEquationType, consumableDescription, consumablePrice, imageIndex);

            // if item rarity key already exists, add newItem to the list, else create new rarity key with a new list of items.
            if (customLootTable.ContainsKey(consumableRarity))
            {
                customLootTable[consumableRarity].Add(newConsumableItem);
            }
            else
            {
                customLootTable.Add(consumableRarity, new List<Consumable> { newConsumableItem });
            }

            imageIndex++;
        }

        return customLootTable;
    }

    private bool TryReadLootLines(string configuredPath, out List<string> lines)
    {
        if (lootTableTextAsset != null)
        {
            lines = SplitLines(lootTableTextAsset.text);
            return lines.Count > 0;
        }

        List<string> attemptedPaths = BuildCandidatePaths(configuredPath);
        for (int i = 0; i < attemptedPaths.Count; i++)
        {
            string candidatePath = attemptedPaths[i];
            if (File.Exists(candidatePath))
            {
                lines = new List<string>(File.ReadAllLines(candidatePath));
                return lines.Count > 0;
            }
        }

        string resourcePath = string.IsNullOrWhiteSpace(lootTableResourcePath)
            ? Path.GetFileNameWithoutExtension(configuredPath)
            : lootTableResourcePath;
        TextAsset resourceCsv = Resources.Load<TextAsset>(resourcePath);
        if (resourceCsv != null)
        {
            lines = SplitLines(resourceCsv.text);
            return lines.Count > 0;
        }

        Debug.LogError($"ConsumableGenerator: Could not load loot table CSV. Checked file paths: {string.Join(" | ", attemptedPaths)} and Resources path '{resourcePath}'.", this);
        lines = new List<string>();
        return false;
    }

    private static List<string> SplitLines(string text)
    {
        List<string> lines = new List<string>();
        using (StringReader reader = new StringReader(text ?? string.Empty))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private static List<string> BuildCandidatePaths(string configuredPath)
    {
        List<string> paths = new List<string>();
        AddPath(paths, configuredPath);

        string normalized = string.IsNullOrWhiteSpace(configuredPath)
            ? string.Empty
            : configuredPath.Replace('\\', '/');

        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            string relativeToAssets = normalized.Substring("Assets/".Length);
            AddPath(paths, Path.Combine(Application.dataPath, relativeToAssets));
            AddPath(paths, Path.Combine(Application.streamingAssetsPath, relativeToAssets));
        }

        string fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            AddPath(paths, Path.Combine(Application.streamingAssetsPath, fileName));
            AddPath(paths, Path.Combine(Application.streamingAssetsPath, "CSV Files", fileName));
        }

        return paths;
    }

    private static void AddPath(List<string> paths, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!paths.Contains(path))
        {
            paths.Add(path);
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