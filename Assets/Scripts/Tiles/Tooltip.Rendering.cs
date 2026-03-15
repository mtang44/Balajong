using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class Tooltip
{
    private EditionVisualData BuildEditionData(MahjongTileData tileData, out bool shouldShow)
    {
        EditionVisualData data = default;

        shouldShow = editionTooltip != null && editionTooltip.isEnabled && tileData != null;
        if (!shouldShow)
        {
            return data;
        }

        Edition edition = tileData.Edition;
        if (hideEditionTooltipForBase && edition == Edition.Base)
        {
            shouldShow = false;
            return data;
        }

        string description = GetEditionDescription(edition);
        if (string.IsNullOrWhiteSpace(description))
        {
            shouldShow = false;
            return data;
        }

        string editionName = GetEditionDisplayName(edition);
        Color textColor = GetEditionTextColor(edition);
        Color badgeColor = GetEditionBackgroundColor(edition);

        int baseFontSize = editionTooltip != null && editionTooltip.panelStyle != null
            ? Mathf.Max(1, Mathf.RoundToInt(editionTooltip.panelStyle.fontSize))
            : 18;
        int editionNameFontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * editionNameFontMultiplier));

        data.Name = editionName;
        data.Description = description;
        data.TextColor = textColor;
        data.BadgeColor = badgeColor;
        data.NameFontSize = editionNameFontSize;
        return data;
    }

    private string GetEditionDisplayName(Edition edition)
    {
        if (TryGetEditionEntry(edition, out EditionDescriptionEntry entry) && !string.IsNullOrWhiteSpace(entry.displayName))
        {
            return entry.displayName.Trim();
        }

        return edition.ToString();
    }

    private int ResolveFaceValue(MahjongTileData tileData)
    {
        if (tileData == null)
        {
            return 0;
        }

        if (ScoringManager.Instance != null)
        {
            return ScoringManager.Instance.GetTileFaceValue(tileData);
        }

        return tileData.TileType switch
        {
            TileType.Dots => (int)tileData.NumberedValue,
            TileType.Bam => (int)tileData.NumberedValue,
            TileType.Crack => (int)tileData.NumberedValue,
            _ => 0
        };
    }

    private string GetEditionDescription(Edition edition)
    {
        if (TryGetEditionEntry(edition, out EditionDescriptionEntry entry) && !string.IsNullOrWhiteSpace(entry.description))
        {
            return entry.description.Trim();
        }

        if (!useDefaultEditionDescriptionsWhenEmpty)
        {
            return string.Empty;
        }

        return GetDefaultEditionDescription(edition);
    }

    private bool TryGetEditionEntry(Edition edition, out EditionDescriptionEntry entry)
    {
        for (int i = 0; i < editionDescriptions.Count; i++)
        {
            if (editionDescriptions[i].edition == edition)
            {
                entry = editionDescriptions[i];
                return true;
            }
        }

        entry = default;
        return false;
    }

    private Color GetEditionTextColor(Edition edition)
    {
        if (TryGetEditionEntry(edition, out EditionDescriptionEntry entry) && entry.useCustomTextColor)
        {
            return entry.textColor;
        }

        if (editionTooltip != null && editionTooltip.panelStyle != null)
        {
            return editionTooltip.panelStyle.textColor;
        }

        return GetDefaultEditionTextColor(edition);
    }

    private Color GetEditionBackgroundColor(Edition edition)
    {
        if (TryGetEditionEntry(edition, out EditionDescriptionEntry entry) && entry.useCustomBackgroundColor)
        {
            return entry.backgroundColor;
        }

        switch (edition)
        {
            case Edition.Ghost: return ghostEditionBgColor;
            case Edition.Enchanted: return enchantedEditionBgColor;
            case Edition.Crystal: return crystalEditionBgColor;
            default: return baseEditionBgColor;
        }
    }

    private static string GetDefaultEditionDescription(Edition edition)
    {
        switch (edition)
        {
            case Edition.Base:
                return "No special edition effect";
            case Edition.Ghost:
                return "Adds +50 points to meld";
            case Edition.Enchanted:
                return "Multiplies meld multiplier by x1.5";
            case Edition.Crystal:
                return "Meld multiplier gains +5";
            default:
                return "Special edition effect";
        }
    }

    private static Color GetDefaultEditionTextColor(Edition edition)
    {
        switch (edition)
        {
            case Edition.Ghost:
                return new Color(0.86f, 1f, 0.96f, 1f);
            case Edition.Enchanted:
                return new Color(0.93f, 0.86f, 1f, 1f);
            case Edition.Crystal:
                return new Color(0.88f, 0.97f, 1f, 1f);
            default:
                return new Color(0.15f, 0.15f, 0.15f, 1f);
        }
    }

    private static Sprite GetOrCreateRoundedSprite(float radius)
    {
        if (sharedRoundedSprite != null && Mathf.Approximately(sharedRoundedSpriteRadius, radius))
        {
            return sharedRoundedSprite;
        }

        if (sharedRoundedSprite != null && sharedRoundedSprite.texture != null)
        {
            UnityEngine.Object.Destroy(sharedRoundedSprite.texture);
        }

        sharedRoundedSprite = CreateRoundedRectSprite(radius);
        sharedRoundedSpriteRadius = radius;
        return sharedRoundedSprite;
    }

    private static Sprite CreateRoundedRectSprite(float cornerRadius)
    {
        const int size = 64;
        float r = Mathf.Clamp(cornerRadius, 0f, size * 0.5f);
        int border = Mathf.Max(1, Mathf.RoundToInt(r));
        float half = size * 0.5f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float qx = Mathf.Abs(px - half) - (half - r);
                float qy = Mathf.Abs(py - half) - (half - r);
                float dist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - r;
                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            1f, 0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    private static string SafeFormat(string format, string fallback, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return fallback;
        }

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private void ConfigureSubTooltip(TooltipPanelRefs panelRefs, SubTooltipStyle style, string text, bool shouldShow)
    {
        if (panelRefs == null || panelRefs.PanelRect == null)
        {
            return;
        }

        panelRefs.PanelRect.gameObject.SetActive(shouldShow);
        if (!shouldShow || style == null)
        {
            SetEditionBadgeActive(panelRefs, false);
            return;
        }

        ApplyPanelVisualSettings(panelRefs, style.panelStyle, tooltipCornerRadius);
        SetEditionBadgeActive(panelRefs, false);
        panelRefs.Text.text = text;
        ResizePanelToFitText(panelRefs, style.panelStyle);
    }

    private void ConfigureEditionSubTooltip(TooltipPanelRefs panelRefs, SubTooltipStyle style, EditionVisualData data, bool shouldShow)
    {
        if (panelRefs == null || panelRefs.PanelRect == null)
        {
            return;
        }

        panelRefs.PanelRect.gameObject.SetActive(shouldShow);
        if (!shouldShow || style == null)
        {
            SetEditionBadgeActive(panelRefs, false);
            return;
        }

        ApplyPanelVisualSettings(panelRefs, style.panelStyle, tooltipCornerRadius);

        panelRefs.Text.text = data.Description;
        panelRefs.Text.color = data.TextColor;

        if (panelRefs.EditionNameBadgeRect == null || panelRefs.EditionNameBadgeImage == null || panelRefs.EditionNameText == null)
        {
            ResizePanelToFitText(panelRefs, style.panelStyle);
            return;
        }

        SetEditionBadgeActive(panelRefs, true);
        panelRefs.EditionNameBadgeImage.color = data.BadgeColor;
        panelRefs.EditionNameBadgeImage.sprite = GetOrCreateRoundedSprite(tooltipCornerRadius);
        panelRefs.EditionNameBadgeImage.type = Image.Type.Sliced;
        panelRefs.EditionNameBadgeImage.pixelsPerUnitMultiplier = 1f;

        panelRefs.EditionNameText.font = style.panelStyle.fontAsset != null ? style.panelStyle.fontAsset : sharedDefaultFontAsset;
        panelRefs.EditionNameText.fontSize = data.NameFontSize;
        panelRefs.EditionNameText.fontStyle = style.panelStyle.fontStyle;
        panelRefs.EditionNameText.alignment = TextAlignmentOptions.Center;
        panelRefs.EditionNameText.color = data.TextColor;
        panelRefs.EditionNameText.enableAutoSizing = false;
        panelRefs.EditionNameText.textWrappingMode = TextWrappingModes.NoWrap;
        panelRefs.EditionNameText.text = data.Name;

        ResizeEditionPanelToFitContent(panelRefs, style.panelStyle);
    }

    private static void ApplyPanelVisualSettings(TooltipPanelRefs panelRefs, PanelStyle style, float cornerRadius)
    {
        if (panelRefs == null || style == null)
        {
            return;
        }

        panelRefs.PanelRect.sizeDelta = style.panelSize;
        panelRefs.TextRect.offsetMin = new Vector2(style.panelPadding.x, style.panelPadding.y);
        panelRefs.TextRect.offsetMax = new Vector2(-style.panelPadding.x, -style.panelPadding.y);

        panelRefs.BackgroundImage.color = style.backgroundColor;
        panelRefs.BackgroundImage.sprite = GetOrCreateRoundedSprite(cornerRadius);
        panelRefs.BackgroundImage.type = Image.Type.Sliced;
        panelRefs.BackgroundImage.pixelsPerUnitMultiplier = 1f;

        bool drawBorder = style.borderThickness > 0f;
        panelRefs.BorderOutline.enabled = drawBorder;
        if (drawBorder)
        {
            panelRefs.BorderOutline.useGraphicAlpha = true;
            panelRefs.BorderOutline.effectColor = style.borderColor;
            panelRefs.BorderOutline.effectDistance = new Vector2(style.borderThickness, style.borderThickness);
        }

        panelRefs.Text.font = style.fontAsset != null ? style.fontAsset : sharedDefaultFontAsset;
        panelRefs.Text.fontSize = style.fontSize;
        panelRefs.Text.fontStyle = style.fontStyle;
        panelRefs.Text.alignment = style.textAlignment;
        panelRefs.Text.color = style.textColor;
        panelRefs.Text.enableAutoSizing = style.autoSizeText;
        panelRefs.Text.textWrappingMode = TextWrappingModes.NoWrap;

        if (style.autoSizeText)
        {
            float minSize = Mathf.Min(style.autoSizeMin, style.autoSizeMax);
            float maxSize = Mathf.Max(style.autoSizeMin, style.autoSizeMax);
            panelRefs.Text.fontSizeMin = minSize;
            panelRefs.Text.fontSizeMax = maxSize;
        }
    }

    private void ResizeEditionPanelToFitContent(TooltipPanelRefs panelRefs, PanelStyle style)
    {
        if (panelRefs == null || style == null)
        {
            return;
        }

        Vector2 badgeTextPreferred = panelRefs.EditionNameText.GetPreferredValues(panelRefs.EditionNameText.text);
        float badgeWidth = Mathf.Max(1f, badgeTextPreferred.x + (editionNameBadgePadding.x * 2f));
        float badgeHeight = Mathf.Max(1f, badgeTextPreferred.y + (editionNameBadgePadding.y * 2f));

        Vector2 descriptionPreferred = panelRefs.Text.GetPreferredValues(panelRefs.Text.text);

        Vector2 resolvedSize = style.panelSize;
        if (style.scalePanelToFitText)
        {
            float requiredWidth = Mathf.Max(badgeWidth, descriptionPreferred.x) + (style.panelPadding.x * 2f);
            float requiredHeight = badgeHeight + editionNameDescriptionGap + descriptionPreferred.y + (style.panelPadding.y * 2f);

            resolvedSize.x = Mathf.Max(style.panelSize.x, requiredWidth);
            resolvedSize.y = Mathf.Max(style.panelSize.y, requiredHeight);

            if (style.maxAutoPanelWidth > 0f)
            {
                float clampedMaxWidth = Mathf.Max(style.panelSize.x, style.maxAutoPanelWidth);
                resolvedSize.x = Mathf.Min(resolvedSize.x, clampedMaxWidth);
            }
        }

        panelRefs.PanelRect.sizeDelta = resolvedSize;
        panelRefs.EditionNameBadgeRect.sizeDelta = new Vector2(badgeWidth, badgeHeight);
        panelRefs.EditionNameBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        panelRefs.EditionNameBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        panelRefs.EditionNameBadgeRect.pivot = new Vector2(0.5f, 1f);
        panelRefs.EditionNameBadgeRect.anchoredPosition = new Vector2(0f, -style.panelPadding.y);

        float topInset = style.panelPadding.y + badgeHeight + editionNameDescriptionGap;
        panelRefs.TextRect.offsetMin = new Vector2(style.panelPadding.x, style.panelPadding.y);
        panelRefs.TextRect.offsetMax = new Vector2(-style.panelPadding.x, -topInset);
    }

    private static void SetEditionBadgeActive(TooltipPanelRefs panelRefs, bool isActive)
    {
        if (panelRefs != null && panelRefs.EditionNameBadgeRect != null)
        {
            panelRefs.EditionNameBadgeRect.gameObject.SetActive(isActive);
        }
    }

    private static void ResizePanelToFitText(TooltipPanelRefs panelRefs, PanelStyle style)
    {
        if (panelRefs == null || style == null)
        {
            return;
        }

        Vector2 resolvedSize = style.panelSize;
        if (style.scalePanelToFitText)
        {
            Vector2 preferredTextSize = panelRefs.Text.GetPreferredValues(panelRefs.Text.text);
            resolvedSize.x = Mathf.Max(style.panelSize.x, preferredTextSize.x + (style.panelPadding.x * 2f));
            resolvedSize.y = Mathf.Max(style.panelSize.y, preferredTextSize.y + (style.panelPadding.y * 2f));

            if (style.maxAutoPanelWidth > 0f)
            {
                float clampedMaxWidth = Mathf.Max(style.panelSize.x, style.maxAutoPanelWidth);
                resolvedSize.x = Mathf.Min(resolvedSize.x, clampedMaxWidth);
            }
        }

        panelRefs.PanelRect.sizeDelta = resolvedSize;
    }

    private void LayoutSubTooltipPanels(bool showFaceTooltip, bool showEditionTooltip)
    {
        if (sharedMainPanel == null || sharedMainPanel.PanelRect == null)
        {
            return;
        }

        sharedMainPanel.PanelRect.anchoredPosition = Vector2.zero;

        Vector2 facePosition = Vector2.zero;
        Vector2 editionPosition = Vector2.zero;

        if (sharedFacePanel != null && sharedFacePanel.PanelRect != null)
        {
            facePosition = showFaceTooltip ? faceValueTooltip.offsetFromMain : Vector2.zero;
            sharedFacePanel.PanelRect.anchoredPosition = facePosition;
        }

        if (sharedEditionPanel != null && sharedEditionPanel.PanelRect != null)
        {
            editionPosition = showEditionTooltip ? editionTooltip.offsetFromMain : Vector2.zero;

            if (showFaceTooltip && showEditionTooltip && avoidSubTooltipOverlap)
            {
                float halfFaceHeight = sharedFacePanel != null && sharedFacePanel.PanelRect != null
                    ? sharedFacePanel.PanelRect.rect.height * 0.5f
                    : 0f;
                float halfEditionHeight = sharedEditionPanel.PanelRect.rect.height * 0.5f;
                float minimumYSeparation = halfFaceHeight + halfEditionHeight + subTooltipVerticalGap;
                float currentYSeparation = Mathf.Abs(editionPosition.y - facePosition.y);
                bool mostlySameHorizontalLane = Mathf.Abs(editionPosition.x - facePosition.x) <= 1f;

                if (mostlySameHorizontalLane && currentYSeparation < minimumYSeparation)
                {
                    float direction = editionPosition.y <= facePosition.y ? -1f : 1f;
                    editionPosition.y = facePosition.y + (direction * minimumYSeparation);
                }
            }

            sharedEditionPanel.PanelRect.anchoredPosition = editionPosition;
        }
    }

    private static void UpdateConnector(
        RectTransform connectorRect,
        Image connectorImage,
        bool shouldShowTargetPanel,
        SubTooltipStyle style,
        RectTransform targetPanelRect)
    {
        if (connectorRect == null || connectorImage == null || style == null || targetPanelRect == null)
        {
            return;
        }

        bool canShow = shouldShowTargetPanel && style.showConnector && style.connectorThickness > 0f;
        if (!canShow)
        {
            connectorRect.gameObject.SetActive(false);
            return;
        }

        Vector2 startPoint = style.connectorStartOffset;
        Vector2 endPoint = targetPanelRect.anchoredPosition + style.connectorEndOffset;
        Vector2 delta = endPoint - startPoint;
        float length = delta.magnitude;

        if (length <= 0.01f)
        {
            connectorRect.gameObject.SetActive(false);
            return;
        }

        connectorRect.gameObject.SetActive(true);
        connectorImage.color = style.connectorColor;
        connectorRect.sizeDelta = new Vector2(length, style.connectorThickness);
        connectorRect.anchoredPosition = startPoint + (delta * 0.5f);
        connectorRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private bool EnsureSharedTooltip()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            return false;
        }

        if (sharedRootRect != null && sharedCanvas != canvas)
        {
            Destroy(sharedRootRect.gameObject);
            ClearSharedTooltipReferences();
        }

        if (sharedRootRect != null)
        {
            return true;
        }

        sharedCanvas = canvas;

        GameObject rootObject = new GameObject("TileTooltipRoot", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.layer = canvas.gameObject.layer;

        sharedRootRect = rootObject.GetComponent<RectTransform>();
        sharedRootRect.SetParent(canvas.transform, false);
        sharedRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        sharedRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        sharedRootRect.pivot = new Vector2(0.5f, 0.5f);

        sharedRootCanvasGroup = rootObject.GetComponent<CanvasGroup>();
        sharedRootCanvasGroup.interactable = false;
        sharedRootCanvasGroup.blocksRaycasts = false;

        sharedFaceConnectorRect = CreateConnector("FaceConnector", sharedRootRect, rootObject.layer, out sharedFaceConnectorImage);
        sharedEditionConnectorRect = CreateConnector("EditionConnector", sharedRootRect, rootObject.layer, out sharedEditionConnectorImage);

        sharedMainPanel = CreateTooltipPanel("MainTooltipPanel", sharedRootRect, rootObject.layer);
        sharedFacePanel = CreateTooltipPanel("FaceValueTooltipPanel", sharedRootRect, rootObject.layer);
        sharedEditionPanel = CreateTooltipPanel("EditionTooltipPanel", sharedRootRect, rootObject.layer, true);

        sharedDefaultFontAsset = sharedMainPanel.Text.font;
        sharedRootRect.gameObject.SetActive(false);
        return true;
    }

    private static void ClearSharedTooltipReferences()
    {
        sharedRootRect = null;
        sharedRootCanvasGroup = null;
        sharedMainPanel = null;
        sharedFacePanel = null;
        sharedEditionPanel = null;
        sharedFaceConnectorRect = null;
        sharedFaceConnectorImage = null;
        sharedEditionConnectorRect = null;
        sharedEditionConnectorImage = null;
        sharedCanvas = null;
        sharedDefaultFontAsset = null;
        activeOwner = null;

        if (sharedRoundedSprite != null && sharedRoundedSprite.texture != null)
        {
            Destroy(sharedRoundedSprite.texture);
        }

        sharedRoundedSprite = null;
        sharedRoundedSpriteRadius = -1f;
    }

    private static TooltipPanelRefs CreateTooltipPanel(string objectName, RectTransform parent, int layer, bool createEditionNameBadge = false)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline));
        panelObject.layer = layer;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.raycastTarget = false;

        Outline panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.useGraphicAlpha = true;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = string.Empty;

        RectTransform editionNameBadgeRect = null;
        Image editionNameBadgeImage = null;
        TextMeshProUGUI editionNameText = null;

        if (createEditionNameBadge)
        {
            GameObject badgeObject = new GameObject("EditionNameBadge", typeof(RectTransform), typeof(Image));
            badgeObject.layer = layer;

            editionNameBadgeRect = badgeObject.GetComponent<RectTransform>();
            editionNameBadgeRect.SetParent(panelRect, false);
            editionNameBadgeRect.anchorMin = new Vector2(0.5f, 1f);
            editionNameBadgeRect.anchorMax = new Vector2(0.5f, 1f);
            editionNameBadgeRect.pivot = new Vector2(0.5f, 1f);

            editionNameBadgeImage = badgeObject.GetComponent<Image>();
            editionNameBadgeImage.raycastTarget = false;

            GameObject badgeTextObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextObject.layer = layer;

            RectTransform badgeTextRect = badgeTextObject.GetComponent<RectTransform>();
            badgeTextRect.SetParent(editionNameBadgeRect, false);
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.pivot = new Vector2(0.5f, 0.5f);
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;

            editionNameText = badgeTextObject.GetComponent<TextMeshProUGUI>();
            editionNameText.raycastTarget = false;
            editionNameText.textWrappingMode = TextWrappingModes.NoWrap;
            editionNameText.text = string.Empty;

            editionNameBadgeRect.gameObject.SetActive(false);
        }

        return new TooltipPanelRefs
        {
            PanelRect = panelRect,
            TextRect = textRect,
            BackgroundImage = panelImage,
            BorderOutline = panelOutline,
            Text = text,
            EditionNameBadgeRect = editionNameBadgeRect,
            EditionNameBadgeImage = editionNameBadgeImage,
            EditionNameText = editionNameText
        };
    }

    private static RectTransform CreateConnector(string objectName, RectTransform parent, int layer, out Image connectorImage)
    {
        GameObject connectorObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        connectorObject.layer = layer;

        RectTransform connectorRect = connectorObject.GetComponent<RectTransform>();
        connectorRect.SetParent(parent, false);
        connectorRect.anchorMin = new Vector2(0.5f, 0.5f);
        connectorRect.anchorMax = new Vector2(0.5f, 0.5f);
        connectorRect.pivot = new Vector2(0.5f, 0.5f);

        connectorImage = connectorObject.GetComponent<Image>();
        connectorImage.raycastTarget = false;

        return connectorRect;
    }
}
