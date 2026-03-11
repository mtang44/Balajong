using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MahjongTileHolder : MonoBehaviour
{
    private const string SpriteSheetPath = "Assets/Art Assets/Tiles/MahjongTilesTransparent.png";
    private const string SpriteNamePrefix = "MahjongTilesTransparent_";

    [SerializeField]
    private MahjongTileData tileData;
    public MahjongTileData TileData => tileData;
    
    private void OnEnable()
    {
        // Initialize tileData if it's null
        if (tileData == null)
        {
            tileData = new MahjongTileData(TileType.Dots, NumberedValue.One);
        }
    }

    private void Start()
    {
        // Look up and set sprite if not already set
        if (tileData.Sprite == null)
        {
            LookupAndSetSprite();
        }
        
        // Apply sprite after dealer has set the values
        MahjongTileDisplay display = GetComponent<MahjongTileDisplay>();
        if (display != null)
        {
            display.ApplyTileSprite();
        }
    }
    
    public void SetTileData(MahjongTileData newData)
    {
        tileData = newData;
        if (tileData != null && tileData.Sprite == null)
        {
            LookupAndSetSprite();
        }

        // Sprite Changes
        MahjongTileDisplay display = GetComponent<MahjongTileDisplay>();
        if (display != null)
        {
            display.ApplyTileSprite();
        }
    }

    public void OnValidate()
    {
        // Notify display component to update when data changes
        MahjongTileDisplay display = GetComponent<MahjongTileDisplay>();
        if (display != null)
        {
            display.ApplyTileSprite();
        }
    }

    private void LookupAndSetSprite()
    {
        if (tileData == null)
            return;

#if UNITY_EDITOR
        int spriteIndex = GetSpriteIndex(
            tileData.TileType,
            tileData.NumberedValue,
            tileData.WindValue,
            tileData.DragonValue,
            tileData.FlowerValue,
            tileData.SeasonValue
        );

        if (spriteIndex < 0)
            return;

        string spriteName = GetSpriteName(spriteIndex);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .ToArray();

        Sprite foundSprite = sprites.FirstOrDefault(s => s.name == spriteName);
        if (foundSprite != null)
        {
            tileData.SetSprite(foundSprite);
        }
#else
        // Runtime fallback: attempt to load from Resources
        int spriteIndex = GetSpriteIndex(
            tileData.TileType,
            tileData.NumberedValue,
            tileData.WindValue,
            tileData.DragonValue,
            tileData.FlowerValue,
            tileData.SeasonValue
        );

        if (spriteIndex < 0)
            return;

        string spriteName = GetSpriteName(spriteIndex);
        // Try to load from sprite atlas or resources
        Sprite sprite = Resources.LoadAll<Sprite>("Tiles").FirstOrDefault(s => s.name == spriteName);
        if (sprite != null)
        {
            tileData.SetSprite(sprite);
        }
#endif
    }

    private static string GetSpriteName(int spriteIndex)
    {
        if (spriteIndex < 0)
            return null;
        return $"{SpriteNamePrefix}{spriteIndex}";
    }

    private static int GetSpriteIndex(
        TileType tileType,
        NumberedValue numberedValue,
        WindValue windValue,
        DragonValue dragonValue,
        FlowerValue flowerValue,
        SeasonValue seasonValue)
    {
        return tileType switch
        {
            TileType.Dots => (int)numberedValue - 1,
            TileType.Crack => 9 + (int)numberedValue - 1,
            TileType.Bam => 18 + (int)numberedValue - 1,
            TileType.Flower => GetFlowerIndex(flowerValue),
            TileType.Season => GetSeasonIndex(seasonValue),
            TileType.Dragon => GetDragonIndex(dragonValue),
            TileType.Wind => GetWindIndex(windValue),
            _ => -1
        };
    }

    private static int GetFlowerIndex(FlowerValue value) => value switch
    {
        FlowerValue.Plum => 27,
        FlowerValue.Orchid => 28,
        FlowerValue.Chrysanthemum => 29,
        FlowerValue.Bamboo => 30,
        _ => -1
    };

    private static int GetSeasonIndex(SeasonValue value) => value switch
    {
        SeasonValue.Spring => 31,
        SeasonValue.Summer => 32,
        SeasonValue.Autumn => 33,
        SeasonValue.Winter => 34,
        _ => -1
    };

    private static int GetDragonIndex(DragonValue value) => value switch
    {
        DragonValue.Red => 36,
        DragonValue.Green => 37,
        DragonValue.White => 38,
        _ => -1
    };

    private static int GetWindIndex(WindValue value) => value switch
    {
        WindValue.East => 39,
        WindValue.South => 40,
        WindValue.West => 41,
        WindValue.North => 42,
        _ => -1
    };
}
