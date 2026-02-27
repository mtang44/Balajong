using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(MahjongTile))]
public class MahjongTileEditor : Editor
{
    private SerializedProperty tileTypeProperty;
    private SerializedProperty numberedValueProperty;
    private SerializedProperty windValueProperty;
    private SerializedProperty dragonValueProperty;
    private SerializedProperty flowerValueProperty;
    private SerializedProperty seasonValueProperty;
    private SerializedProperty spriteProperty;

    private void OnEnable()
    {
        tileTypeProperty = serializedObject.FindProperty("tileType");
        numberedValueProperty = serializedObject.FindProperty("numberedValue");
        windValueProperty = serializedObject.FindProperty("windValue");
        dragonValueProperty = serializedObject.FindProperty("dragonValue");
        flowerValueProperty = serializedObject.FindProperty("flowerValue");
        seasonValueProperty = serializedObject.FindProperty("seasonValue");
        spriteProperty = serializedObject.FindProperty("sprite");
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

        // Auto-update sprite when any value changes
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
        MahjongTile tile = target as MahjongTile;
        if (tile == null)
            return;

        int spriteIndex = MahjongSpriteMapping.GetSpriteIndex(
            tile.TileType,
            tile.NumberedValue,
            tile.WindValue,
            tile.DragonValue,
            tile.FlowerValue,
            tile.SeasonValue
        );

        Debug.Log($"Tile Type: {tile.TileType}, Sprite Index: {spriteIndex}");

        if (spriteIndex < 0)
        {
            Debug.LogWarning("Invalid tile configuration for sprite mapping.", tile);
            spriteProperty.objectReferenceValue = null;
            return;
        }

        string spriteName = MahjongSpriteMapping.GetSpriteName(spriteIndex);
        Debug.Log($"Looking for sprite: {spriteName}");

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art Assets/Tiles/MahjongTiles.png")
            .OfType<Sprite>()
            .ToArray();

        Debug.Log($"Found {sprites.Length} sprites in MahjongTiles.png");
        if (sprites.Length > 0)
        {
            Debug.Log($"First 5 sprite names: {string.Join(", ", sprites.Take(5).Select(s => s.name))}");
        }

        Sprite foundSprite = sprites.FirstOrDefault(s => s.name == spriteName);

        if (foundSprite != null)
        {
            Debug.Log($"Successfully found sprite: {spriteName}");
            spriteProperty.objectReferenceValue = foundSprite;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(tile);
        }
        else
        {
            Debug.LogWarning($"Sprite '{spriteName}' not found in MahjongTiles.png. Available sprites: {string.Join(", ", sprites.Select(s => s.name))}", tile);
        }
    }
}
