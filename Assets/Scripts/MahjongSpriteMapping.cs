using UnityEngine;

public static class MahjongSpriteMapping
{
    private const string SPRITE_SHEET_PATH = "Assets/Art Assets/Tiles/MahjongTiles.png";
    private const string SPRITE_NAME_PREFIX = "MahjongTiles_";

    /// <summary>
    /// Gets the sprite index for a given tile type and value.
    /// </summary>
    public static int GetSpriteIndex(TileType tileType, NumberedValue numberedValue, WindValue windValue, DragonValue dragonValue, FlowerValue flowerValue, SeasonValue seasonValue)
    {
        return tileType switch
        {
            TileType.Dots => (int)numberedValue - 1, // 0-8
            TileType.Crack => 9 + (int)numberedValue - 1, // 9-17
            TileType.Bam => 18 + (int)numberedValue - 1, // 18-26
            TileType.Flower => GetFlowerIndex(flowerValue), // 27-30
            TileType.Season => GetSeasonIndex(seasonValue), // 31-34
            TileType.Dragon => GetDragonIndex(dragonValue), // 36-38
            TileType.Wind => GetWindIndex(windValue), // 39-42
            _ => -1
        };
    }

    /// <summary>
    /// Gets the sprite name for a given sprite index.
    /// </summary>
    public static string GetSpriteName(int spriteIndex)
    {
        if (spriteIndex < 0)
            return null;
        
        return $"{SPRITE_NAME_PREFIX}{spriteIndex}";
    }

    private static int GetFlowerIndex(FlowerValue value)
    {
        return value switch
        {
            FlowerValue.Plum => 27,
            FlowerValue.Orchid => 28,
            FlowerValue.Chrysanthemum => 29,
            FlowerValue.Bamboo => 30,
            _ => -1
        };
    }

    private static int GetSeasonIndex(SeasonValue value)
    {
        return value switch
        {
            SeasonValue.Spring => 31,
            SeasonValue.Summer => 32,
            SeasonValue.Autumn => 33,
            SeasonValue.Winter => 34,
            _ => -1
        };
    }

    private static int GetDragonIndex(DragonValue value)
    {
        return value switch
        {
            DragonValue.Red => 36,
            DragonValue.Green => 37,
            DragonValue.White => 38,
            _ => -1
        };
    }

    private static int GetWindIndex(WindValue value)
    {
        return value switch
        {
            WindValue.East => 39,
            WindValue.South => 40,
            WindValue.West => 41,
            WindValue.North => 42,
            _ => -1
        };
    }
}
