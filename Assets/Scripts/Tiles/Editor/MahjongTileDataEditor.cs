using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(MahjongTileHolder))]
public class MahjongTileDataEditor : Editor
{
    private const string SpriteSheetPath = "Assets/Art Assets/Tiles/MahjongTilesTransparent.png";
    private const string SpriteNamePrefix = "MahjongTilesTransparent_";

    private SerializedProperty tileTypeProperty;
    private SerializedProperty numberedValueProperty;
    private SerializedProperty windValueProperty;
    private SerializedProperty dragonValueProperty;
    private SerializedProperty flowerValueProperty;
    private SerializedProperty seasonValueProperty;
    private SerializedProperty spriteProperty;

    private void OnEnable()
    {
        tileTypeProperty = serializedObject.FindProperty("tileData.tileType");
        numberedValueProperty = serializedObject.FindProperty("tileData.numberedValue");
        windValueProperty = serializedObject.FindProperty("tileData.windValue");
        dragonValueProperty = serializedObject.FindProperty("tileData.dragonValue");
        flowerValueProperty = serializedObject.FindProperty("tileData.flowerValue");
        seasonValueProperty = serializedObject.FindProperty("tileData.seasonValue");
        spriteProperty = serializedObject.FindProperty("tileData.sprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        // Draw Tile Type dropdown
        EditorGUILayout.PropertyField(tileTypeProperty, new GUIContent("Tile Type"));

        EditorGUILayout.Space();

        // Draw the appropriate value dropdown based on tile type
        TileType selectedType = (TileType)tileTypeProperty.enumValueIndex;

        switch (selectedType)
        {
            case TileType.Dots:
                EditorGUILayout.PropertyField(numberedValueProperty, new GUIContent("Value"));
                break;
            case TileType.Bam:
                EditorGUILayout.PropertyField(numberedValueProperty, new GUIContent("Value"));
                break;
            case TileType.Crack:
                EditorGUILayout.PropertyField(numberedValueProperty, new GUIContent("Value"));
                break;
            case TileType.Wind:
                EditorGUILayout.PropertyField(windValueProperty, new GUIContent("Direction"));
                break;
            case TileType.Dragon:
                EditorGUILayout.PropertyField(dragonValueProperty, new GUIContent("Color"));
                break;
            case TileType.Flower:
                EditorGUILayout.PropertyField(flowerValueProperty, new GUIContent("Flower Type"));
                break;
            case TileType.Season:
                EditorGUILayout.PropertyField(seasonValueProperty, new GUIContent("Season"));
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            UpdateSprite();
            serializedObject.Update();
        }

        EditorGUILayout.Space();

        // Display the sprite
        EditorGUILayout.PropertyField(spriteProperty, new GUIContent("Sprite"));

        // Display sprite preview
        if (spriteProperty.objectReferenceValue != null)
        {
            Sprite sprite = spriteProperty.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                EditorGUILayout.LabelField("Preview:");
                Rect previewRect = GUILayoutUtility.GetRect(61, 82, GUILayout.ExpandWidth(false));
                
                // Calculate normalized UV coordinates
                Rect texCoords = new Rect(
                    sprite.textureRect.x / sprite.texture.width,
                    sprite.textureRect.y / sprite.texture.height,
                    sprite.textureRect.width / sprite.texture.width,
                    sprite.textureRect.height / sprite.texture.height
                );
                
                GUI.DrawTextureWithTexCoords(previewRect, sprite.texture, texCoords);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateSprite()
    {
        MahjongTileHolder holder = target as MahjongTileHolder;
        if (holder == null)
            return;
        
        MahjongTileData tileData = holder.TileData;
        if (tileData == null)
            return;

        int spriteIndex = GetSpriteIndex(
            tileData.TileType,
            tileData.NumberedValue,
            tileData.WindValue,
            tileData.DragonValue,
            tileData.FlowerValue,
            tileData.SeasonValue
        );

        if (spriteIndex < 0)
        {
            spriteProperty.objectReferenceValue = null;
            return;
        }

        string spriteName = GetSpriteName(spriteIndex);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .ToArray();

        Sprite foundSprite = sprites.FirstOrDefault(s => s.name == spriteName);
        if (foundSprite != null)
        {
            spriteProperty.objectReferenceValue = foundSprite;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(holder);
        }
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
