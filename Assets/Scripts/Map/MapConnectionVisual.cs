using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct MapConnectionVisualSettings
{
    [Min(1f)] public float lineWidth;
    [Min(1f)] public float dottedCycleLength;
    [Range(0.05f, 0.95f)] public float dottedFillRatio;
    [Min(0f)] public float activeScrollSpeed;
}

public class MapConnectionVisual : MonoBehaviour
{
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private RawImage lineImage;

    private MapConnectionVisualSettings settings;
    private bool isConfigured;
    private bool scrolls;
    private float uvWidth;

    private static readonly Dictionary<int, Texture2D> TextureCache = new Dictionary<int, Texture2D>();

    private void Update()
    {
        if (!isConfigured || !scrolls || lineImage == null || settings.activeScrollSpeed <= 0f)
        {
            return;
        }

        Rect uvRect = lineImage.uvRect;
        uvRect.x -= settings.activeScrollSpeed * Time.unscaledDeltaTime;
        uvRect.width = uvWidth;
        lineImage.uvRect = uvRect;
    }

    public void Setup(MapConnectionVisualSettings style)
    {
        settings = Normalize(style);
        CacheComponents();

        if (lineRect == null || lineImage == null)
        {
            return;
        }

        ConfigureCenteredRect(lineRect);
        lineImage.texture = GetOrCreateDottedTexture(settings.dottedFillRatio);
        lineImage.raycastTarget = false;
        lineImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        isConfigured = true;
    }

    public void SetConnection(Vector2 from, Vector2 to, Color color, bool shouldScroll)
    {
        if (!isConfigured)
        {
            Setup(settings);
        }

        if (lineRect == null || lineImage == null)
        {
            return;
        }

        scrolls = shouldScroll;
        PositionRect(from, to);
        uvWidth = ComputeUvWidth(from, to);

        lineImage.color = color;

        Rect uvRect = lineImage.uvRect;
        uvRect.width = uvWidth;
        if (!scrolls)
        {
            uvRect.x = 0f;
        }

        lineImage.uvRect = uvRect;
    }

    private void CacheComponents()
    {
        lineRect ??= GetComponent<RectTransform>();
        lineImage ??= GetComponent<RawImage>();
    }

    private void PositionRect(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float length = Mathf.Max(1f, delta.magnitude);
        float width = Mathf.Max(1f, settings.lineWidth);

        lineRect.sizeDelta = new Vector2(length, width);
        lineRect.anchoredPosition = (from + to) * 0.5f;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private float ComputeUvWidth(Vector2 from, Vector2 to)
    {
        float cycleLength = Mathf.Max(1f, settings.dottedCycleLength);
        float lineLength = Mathf.Max(1f, Vector2.Distance(from, to));
        return Mathf.Max(1f, lineLength / cycleLength);
    }

    private static void ConfigureCenteredRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Texture2D GetOrCreateDottedTexture(float fillRatio)
    {
        int cacheKey = Mathf.RoundToInt(Mathf.Clamp01(fillRatio) * 1000f);
        if (TextureCache.TryGetValue(cacheKey, out Texture2D cachedTexture) && cachedTexture != null)
        {
            return cachedTexture;
        }

        const int width = 64;
        const int height = 4;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = $"GeneratedDottedLineTexture_{cacheKey}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };

        int filledPixels = Mathf.Clamp(Mathf.RoundToInt(width * Mathf.Clamp01(fillRatio)), 1, width);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isFilled = x < filledPixels;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, isFilled ? 1f : 0f));
            }
        }

        texture.Apply();
        TextureCache[cacheKey] = texture;
        return texture;
    }

    private static MapConnectionVisualSettings Normalize(MapConnectionVisualSettings style)
    {
        style.lineWidth = Mathf.Max(1f, style.lineWidth);
        style.dottedCycleLength = Mathf.Max(1f, style.dottedCycleLength);
        style.dottedFillRatio = Mathf.Clamp(style.dottedFillRatio, 0.05f, 0.95f);
        style.activeScrollSpeed = Mathf.Max(0f, style.activeScrollSpeed);
        return style;
    }
}