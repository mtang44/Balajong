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

    private const string TextureProperty = "_BaseMap";

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        InitializeMaterials();
        ApplyTileSpriteDelayed();
    }

    private IEnumerator ApplyTileSpriteDelayedEnumerator()
    {
        yield return new WaitForEndOfFrame();
        ApplyTileSprite();
    }
    public void ApplyTileSpriteDelayed()
    {
        StartCoroutine(ApplyTileSpriteDelayedEnumerator());
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            ApplyTileSprite();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyTileSprite();
        }
    }

    public void ApplyTileSprite()
    {
        if (targetRenderer == null)
            return;

        MahjongTileHolder holder = GetComponent<MahjongTileHolder>();
        if (holder == null || holder.TileData == null)
            return;

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
