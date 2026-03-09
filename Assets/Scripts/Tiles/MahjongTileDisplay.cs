using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class MahjongTileDisplay : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private int materialIndex = 0;

    [SerializeField]
    private Material materialInstance;

    [SerializeField]
    [Min(0.01f)]
    private float uvScaleMultiplier = 1f;

    [SerializeField]
    private Vector2 uvOffset;

    [SerializeField]
    [Min(0)]
    private int texturePadding = 10;

    [Header("Edition Materials")]
    [SerializeField]
    [Min(0)]
    private int editionMaterialIndex = 1;

    [SerializeField]
    private Material[] editionMaterials = new Material[4];

    private const string TextureProperty = "_BaseMap";

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        EnsureEditionMaterialSlots();
        InitializeMaterials();
    }

    private void OnValidate()
    {
        EnsureEditionMaterialSlots();
        ApplyTileSprite();
    }

    public void ApplyTileSprite()
    {
        if (targetRenderer == null)
            return;

        MahjongTileHolder holder = GetComponent<MahjongTileHolder>();
        if (holder == null || holder.TileData == null)
            return;

        ApplyEditionMaterial(holder.TileData);

        Sprite sprite = holder.TileData.Sprite;
        if (sprite == null)
            return;

        Material baseMaterial = GetBaseMaterial();
        if (baseMaterial == null)
            return;

        Material instance = GetOrCreateInstance(baseMaterial);
        ApplySpriteToMaterial(instance, sprite);
        AssignMaterial(instance);
    }

    private void EnsureEditionMaterialSlots()
    {
        int editionCount = System.Enum.GetValues(typeof(Edition)).Length;
        if (editionMaterials == null || editionMaterials.Length != editionCount)
        {
            Material[] resized = new Material[editionCount];
            if (editionMaterials != null)
            {
                int copyCount = Mathf.Min(editionMaterials.Length, editionCount);
                for (int i = 0; i < copyCount; i++)
                {
                    resized[i] = editionMaterials[i];
                }
            }

            editionMaterials = resized;
        }

        // Default unset entries to Base material once Base is assigned.
        Material baseMaterial = editionMaterials[(int)Edition.Base];
        if (baseMaterial == null)
            return;

        for (int i = 0; i < editionMaterials.Length; i++)
        {
            if (editionMaterials[i] == null)
            {
                editionMaterials[i] = baseMaterial;
            }
        }
    }

    private void ApplyEditionMaterial(MahjongTileData tileData)
    {
        Material editionMaterial = GetEditionMaterial(tileData);
        if (editionMaterial == null)
            return;

        Material[] materials = Application.isPlaying ? targetRenderer.materials : targetRenderer.sharedMaterials;
        if (materials == null || editionMaterialIndex < 0 || editionMaterialIndex >= materials.Length)
            return;

        materials[editionMaterialIndex] = editionMaterial;

        if (Application.isPlaying)
        {
            targetRenderer.materials = materials;
        }
        else
        {
            targetRenderer.sharedMaterials = materials;
        }
    }

    private Material GetEditionMaterial(MahjongTileData tileData)
    {
        if (editionMaterials == null || editionMaterials.Length == 0)
            return null;

        int editionIndex = (int)tileData.Edition;
        if (editionIndex >= 0 && editionIndex < editionMaterials.Length && editionMaterials[editionIndex] != null)
        {
            return editionMaterials[editionIndex];
        }

        int baseIndex = (int)Edition.Base;
        if (baseIndex >= 0 && baseIndex < editionMaterials.Length)
        {
            return editionMaterials[baseIndex];
        }

        return null;
    }

    private Material GetBaseMaterial()
    {
        Material[] materials = Application.isPlaying ? targetRenderer.materials : targetRenderer.sharedMaterials;
        if (materials == null || materialIndex < 0 || materialIndex >= materials.Length)
            return null;

        return materials[materialIndex];
    }

    private Material GetOrCreateInstance(Material baseMaterial)
    {
        if (materialInstance == null || materialInstance.shader != baseMaterial.shader)
        {
            materialInstance = new Material(baseMaterial)
            {
                name = baseMaterial.name + " (Instance)"
            };
        }

        return materialInstance;
    }

    private void AssignMaterial(Material instance)
    {
        Material[] materials = Application.isPlaying ? targetRenderer.materials : targetRenderer.sharedMaterials;
        if (materials == null || materialIndex < 0 || materialIndex >= materials.Length)
            return;

        materials[materialIndex] = instance;
        
        if (Application.isPlaying)
        {
            targetRenderer.materials = materials;
        }
        else
        {
            targetRenderer.sharedMaterials = materials;
        }
    }

    private void InitializeMaterials()
    {
        if (!Application.isPlaying)
            return;

        Material baseMaterial = targetRenderer.sharedMaterials[materialIndex];
        Material newInstance = new Material(baseMaterial)
        {
            name = baseMaterial.name + " (Instance)"
        };
        
        Material[] materials = targetRenderer.materials;
        materials[materialIndex] = newInstance;
        targetRenderer.materials = materials;
        materialInstance = newInstance;
    }

    private void ApplySpriteToMaterial(Material material, Sprite sprite)
    {
        Texture2D croppedTexture = CreateTextureFromSprite(sprite);
        
        material.SetTexture(TextureProperty, croppedTexture);
        material.SetTextureScale(TextureProperty, Vector2.one * uvScaleMultiplier);
        material.SetTextureOffset(TextureProperty, uvOffset);
    }

    private Texture2D CreateTextureFromSprite(Sprite sprite)
    {
        Rect rect = sprite.textureRect;
        int spriteWidth = (int)rect.width;
        int spriteHeight = (int)rect.height;
        
        int paddedWidth = spriteWidth + texturePadding * 2;
        int paddedHeight = spriteHeight + texturePadding * 2;

        Texture2D croppedTexture = new Texture2D(paddedWidth, paddedHeight, TextureFormat.RGBA32, false);
        
        // Fill entire texture with transparent pixels
        Color[] transparentPixels = new Color[paddedWidth * paddedHeight];
        for (int i = 0; i < transparentPixels.Length; i++)
        {
            transparentPixels[i] = Color.clear;
        }
        croppedTexture.SetPixels(transparentPixels);
        
        // Get sprite pixels
        Color[] spritePixels = sprite.texture.GetPixels(
            (int)rect.x,
            (int)rect.y,
            spriteWidth,
            spriteHeight
        );
        
        // Set sprite pixels in the center with padding
        croppedTexture.SetPixels(texturePadding, texturePadding, spriteWidth, spriteHeight, spritePixels);
        croppedTexture.Apply();
        croppedTexture.name = sprite.name;

        return croppedTexture;
    }
}
