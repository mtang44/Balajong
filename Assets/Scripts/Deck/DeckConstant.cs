using UnityEngine;
using System.Collections.Generic;

//This class will handle the starting deck state. It is constant.
public class DeckConstant : MonoBehaviour
{
    //the nomenclature for this I am using is X-Y-Z
    // X is the NUMBER in the deck
    // Y is the VALUE (1-9 for numbered, 1-4 are N/E/S/W, 1-4 for Flowers/Seasons, 1-3 to R/G/W for dragons)
    // Z is the SUIT (C for crack, B for bam, O for dots, D for Dragon, W for Wind, F for Flower, S for Season)
    public static List<string> startingDeckState = new List<string> {
        "41C", "42C", "43C", "44C", "45C", "46C", "47C", "48C", "49C", //CRACK
        "41B", "42B", "43B", "44B", "45B", "46B", "47B", "48B", "49B", //BAM
        "41O", "42O", "43O", "44O", "45O", "46O", "47O", "48O", "49O", //DOTS
        "41W", "42W", "43W", "44W", //WINDS
        "41D", "42D", "43D", //DRAGONS
        "11F", "12F", "13F", "14F", //FLOWERS
        "11S", "12S", "13S", "14S" //SEASONS
        };

    public static List<string> CreateDeckData()
    {
        List<string> newDeckData = new List<string>();
        foreach (string tileString in startingDeckState)
        {
            int count = int.Parse(tileString[0].ToString());
            string value = tileString.Substring(1, tileString.Length - 2);
            string suit = tileString.Substring(tileString.Length - 1, 1);
            
            for (int i = 0; i < count; i++)
            {
                newDeckData.Add(value + suit);
            }
        }
        return newDeckData;
    }

    public static void ConfigureTileData(MahjongTileData tileData, string value, string suit)
    {
        int val = int.Parse(value);
        
        // Use reflection to set private fields
        var tileTypeField = typeof(MahjongTileData).GetField("tileType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var numberedValueField = typeof(MahjongTileData).GetField("numberedValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var windValueField = typeof(MahjongTileData).GetField("windValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dragonValueField = typeof(MahjongTileData).GetField("dragonValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var flowerValueField = typeof(MahjongTileData).GetField("flowerValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seasonValueField = typeof(MahjongTileData).GetField("seasonValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        switch (suit)
        {
            case "C": // Crack
                tileTypeField?.SetValue(tileData, TileType.Crack);
                numberedValueField?.SetValue(tileData, (NumberedValue)val);
                break;
            case "B": // Bam
                tileTypeField?.SetValue(tileData, TileType.Bam);
                numberedValueField?.SetValue(tileData, (NumberedValue)val);
                break;
            case "O": // Dots (O for dOts)
                tileTypeField?.SetValue(tileData, TileType.Dots);
                numberedValueField?.SetValue(tileData, (NumberedValue)val);
                break;
            case "W": // Wind
                tileTypeField?.SetValue(tileData, TileType.Wind);
                windValueField?.SetValue(tileData, (WindValue)(val - 1)); // 1-4 maps to enum 0-3
                break;
            case "D": // Dragon
                tileTypeField?.SetValue(tileData, TileType.Dragon);
                dragonValueField?.SetValue(tileData, (DragonValue)(val - 1)); // 1-3 maps to enum 0-2
                break;
            case "F": // Flower
                tileTypeField?.SetValue(tileData, TileType.Flower);
                flowerValueField?.SetValue(tileData, (FlowerValue)(val - 1)); // 1-4 maps to enum 0-3
                break;
            case "S": // Season
                tileTypeField?.SetValue(tileData, TileType.Season);
                seasonValueField?.SetValue(tileData, (SeasonValue)(val - 1)); // 1-4 maps to enum 0-3
                break;
        }
    }
}
