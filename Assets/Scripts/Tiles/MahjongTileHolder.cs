using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MahjongTileHolder : MonoBehaviour
{
    private const string SpriteSheetPath = "Assets/Art Assets/Tiles/MahjongTilesTransparent.png";
    private const string SpriteNamePrefix = "MahjongTilesTransparent_";
    private const int AtlasColumns = 9;
    private const int AtlasRows = 5;
    private const int AtlasSpriteWidth = 61;
    private const int AtlasSpriteHeight = 82;
    private const int AtlasStepX = 69;
    private const int AtlasStepY = 91;
    private const int AtlasMaxSpriteIndex = 43;
    private static readonly Dictionary<int, Sprite> RuntimeSpriteCache = new Dictionary<int, Sprite>();
    private static Texture2D sharedRuntimeAtlasTexture;

    [SerializeField]
    private MahjongTileData tileData;
    public MahjongTileData TileData => tileData;

    [SerializeField]
    private Sprite runtimeAtlasSourceSprite;
    
    private void OnEnable()
    {
        // Initialize tileData if it's null
        if (tileData == null)
        {
            tileData = new MahjongTileData(TileType.Dots, NumberedValue.One);
        }

        CacheRuntimeAtlasSource(tileData.Sprite);
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
        CacheRuntimeAtlasSource(tileData != null ? tileData.Sprite : null);

        tileData = newData;
        CacheRuntimeAtlasSource(tileData != null ? tileData.Sprite : null);

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

        TryAssignRuntimeAtlasSprite(spriteIndex);
#endif
    }

    private void CacheRuntimeAtlasSource(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        if (runtimeAtlasSourceSprite == null)
        {
            runtimeAtlasSourceSprite = sprite;
        }

        if (sprite.texture != null)
        {
            sharedRuntimeAtlasTexture = sprite.texture;
        }
    }

    private bool TryAssignRuntimeAtlasSprite(int spriteIndex)
    {
        if (tileData == null)
        {
            return false;
        }

        if (RuntimeSpriteCache.TryGetValue(spriteIndex, out Sprite cachedSprite) && cachedSprite != null)
        {
            tileData.SetSprite(cachedSprite);
            return true;
        }

        Texture2D atlasTexture = ResolveRuntimeAtlasTexture();
        if (atlasTexture == null)
        {
            return false;
        }

        if (!TryGetAtlasRect(spriteIndex, out Rect atlasRect))
        {
            return false;
        }

        if (atlasRect.xMax > atlasTexture.width || atlasRect.yMax > atlasTexture.height)
        {
            return false;
        }

        Sprite generatedSprite = Sprite.Create(
            atlasTexture,
            atlasRect,
            new Vector2(0.5f, 0.5f),
            100f);
        generatedSprite.name = GetSpriteName(spriteIndex);

        RuntimeSpriteCache[spriteIndex] = generatedSprite;
        tileData.SetSprite(generatedSprite);
        return true;
    }

    private Texture2D ResolveRuntimeAtlasTexture()
    {
        if (sharedRuntimeAtlasTexture != null)
        {
            return sharedRuntimeAtlasTexture;
        }

        if (runtimeAtlasSourceSprite != null && runtimeAtlasSourceSprite.texture != null)
        {
            sharedRuntimeAtlasTexture = runtimeAtlasSourceSprite.texture;
            return sharedRuntimeAtlasTexture;
        }

        if (tileData != null && tileData.Sprite != null && tileData.Sprite.texture != null)
        {
            sharedRuntimeAtlasTexture = tileData.Sprite.texture;
            return sharedRuntimeAtlasTexture;
        }

        return null;
    }

    private static bool TryGetAtlasRect(int spriteIndex, out Rect rect)
    {
        if (spriteIndex < 0 || spriteIndex > AtlasMaxSpriteIndex)
        {
            rect = default;
            return false;
        }

        int rowFromTop = spriteIndex / AtlasColumns;
        int column = spriteIndex % AtlasColumns;
        int rowFromBottom = (AtlasRows - 1) - rowFromTop;

        rect = new Rect(
            column * AtlasStepX,
            rowFromBottom * AtlasStepY,
            AtlasSpriteWidth,
            AtlasSpriteHeight);
        return true;
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
