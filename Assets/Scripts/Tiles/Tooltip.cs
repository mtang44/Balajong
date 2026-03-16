using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Serializable]
    private class PanelStyle
    {
        public Vector2 panelSize = new Vector2(220f, 52f);
        public Vector2 panelPadding = new Vector2(12f, 8f);
        public bool scalePanelToFitText = true;
        [Min(0f)] public float maxAutoPanelWidth = 0f;

        public TMP_FontAsset fontAsset;
        [Min(1f)] public float fontSize = 24f;
        public FontStyles fontStyle = FontStyles.Bold;
        public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
        public Color textColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);

        public bool autoSizeText = false;
        [Min(1f)] public float autoSizeMin = 14f;
        [Min(1f)] public float autoSizeMax = 32f;

        public Color backgroundColor = Color.white;
        public Color borderColor = new Color(0f, 0f, 0f, 0.35f);
        [Min(0f)] public float borderThickness = 1f;

        public void ClampValues()
        {
            panelSize.x = Mathf.Max(1f, panelSize.x);
            panelSize.y = Mathf.Max(1f, panelSize.y);
            panelPadding.x = Mathf.Max(0f, panelPadding.x);
            panelPadding.y = Mathf.Max(0f, panelPadding.y);
            maxAutoPanelWidth = Mathf.Max(0f, maxAutoPanelWidth);
            fontSize = Mathf.Max(1f, fontSize);
            autoSizeMin = Mathf.Max(1f, autoSizeMin);
            autoSizeMax = Mathf.Max(1f, autoSizeMax);
            borderThickness = Mathf.Max(0f, borderThickness);
        }
    }

    [Serializable]
    private class SubTooltipStyle
    {
        public bool isEnabled = true;
        public Vector2 offsetFromMain = new Vector2(0f, -64f);
        public PanelStyle panelStyle = new PanelStyle();

        public bool showConnector = true;
        public Color connectorColor = new Color(0f, 0f, 0f, 0.35f);
        [Min(0f)] public float connectorThickness = 2f;
        public Vector2 connectorStartOffset = new Vector2(0f, -26f);
        public Vector2 connectorEndOffset = new Vector2(0f, 22f);

        public void ClampValues()
        {
            if (panelStyle == null)
            {
                panelStyle = new PanelStyle();
            }

            panelStyle.ClampValues();
            connectorThickness = Mathf.Max(0f, connectorThickness);
        }
    }

    [Serializable]
    private struct EditionDescriptionEntry
    {
        public Edition edition;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public bool useCustomTextColor;
        public Color textColor;
        public bool useCustomBackgroundColor;
        public Color backgroundColor;
    }

    private sealed class TooltipPanelRefs
    {
        public RectTransform PanelRect;
        public RectTransform TextRect;
        public Image BackgroundImage;
        public Outline BorderOutline;
        public TextMeshProUGUI Text;

        public RectTransform EditionNameBadgeRect;
        public Image EditionNameBadgeImage;
        public TextMeshProUGUI EditionNameText;
    }

    private struct EditionVisualData
    {
        public string Name;
        public string Description;
        public Color TextColor;
        public Color BadgeColor;
        public int NameFontSize;
    }

    [Header("Tile Data")]
    [SerializeField] private MahjongTileHolder tileHolder;
    [SerializeField] private bool hideWhenTileDataMissing = true;

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera worldCameraOverride;

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private Vector2 editionActiveScreenOffset = new Vector2(0f, 80f);
    [SerializeField, Min(0f)] private float horizontalEdgePadding = 16f;

    [Header("Main Tooltip")]
    [SerializeField] private PanelStyle mainTooltipStyle = new PanelStyle();

    [Header("Face Value Sub-Tooltip")]
    [SerializeField] private SubTooltipStyle faceValueTooltip;
    [SerializeField] private string faceValueTextFormat = "+{0} Points";

    [Header("Sub-Tooltip Layout")]
    [SerializeField] private bool avoidSubTooltipOverlap = true;
    [SerializeField, Min(0f)] private float subTooltipVerticalGap = 8f;

    [Header("Corner Rounding")]
    [SerializeField, FormerlySerializedAs("editionCornerRadius"), Min(0f)] private float tooltipCornerRadius = 10f;

    [Header("Edition Sub-Tooltip")]
    [SerializeField] private SubTooltipStyle editionTooltip;
    [SerializeField, Min(0.1f)] private float editionNameFontMultiplier = 1.2f;
    [SerializeField] private bool hideEditionTooltipForBase = true;
    [SerializeField] private bool useDefaultEditionDescriptionsWhenEmpty = true;
    [SerializeField] private Vector2 editionNameBadgePadding = new Vector2(10f, 4f);
    [SerializeField, Min(0f)] private float editionNameDescriptionGap = 6f;

    [Header("Edition Background Colors")]
    [SerializeField] private Color ghostEditionBgColor = new Color(0.06f, 0.47f, 0.45f, 0.85f);
    [SerializeField] private Color enchantedEditionBgColor = new Color(0.39f, 0.19f, 0.58f, 0.85f);
    [SerializeField] private Color crystalEditionBgColor = new Color(0.11f, 0.37f, 0.63f, 0.85f);
    [SerializeField] private Color baseEditionBgColor = Color.clear;

    [SerializeField] private List<EditionDescriptionEntry> editionDescriptions = new List<EditionDescriptionEntry>();

    [Header("Fallback")]
    [SerializeField] private bool useMouseHoverFallback = false;

    private bool isHovering;
    private bool hasLoggedMissingData;
    private bool currentShowsEdition;

    private static RectTransform sharedRootRect;
    private static CanvasGroup sharedRootCanvasGroup;
    private static TooltipPanelRefs sharedMainPanel;
    private static TooltipPanelRefs sharedFacePanel;
    private static TooltipPanelRefs sharedEditionPanel;
    private static RectTransform sharedFaceConnectorRect;
    private static Image sharedFaceConnectorImage;
    private static RectTransform sharedEditionConnectorRect;
    private static Image sharedEditionConnectorImage;
    private static Canvas sharedCanvas;
    private static TMP_FontAsset sharedDefaultFontAsset;
    private static Tooltip activeOwner;
    private static Sprite sharedRoundedSprite;
    private static float sharedRoundedSpriteRadius = -1f;

    private void Awake()
    {
        EnsureSettingsInitialized();

        if (tileHolder == null)
        {
            tileHolder = GetComponent<MahjongTileHolder>();
        }
    }

    private void LateUpdate()
    {
        if (isHovering && activeOwner == this)
        {
            UpdateTooltipPosition();
        }
    }

    private void OnDisable()
    {
        StopHoverAndHideIfOwner();
    }

    private void OnDestroy()
    {
        StopHoverAndHideIfOwner();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHoverAndHideIfOwner();
    }

    private void OnMouseEnter()
    {
        if (useMouseHoverFallback)
        {
            ShowTooltip();
        }
    }

    private void OnMouseExit()
    {
        if (useMouseHoverFallback)
        {
            StopHoverAndHideIfOwner();
        }
    }

    private void OnValidate()
    {
        EnsureSettingsInitialized();
        ClampAllSettings();

        if (activeOwner == this && sharedRootRect != null)
        {
            ShowTooltip();
        }
    }

    private void ShowTooltip()
    {
        EnsureSettingsInitialized();

        MahjongTileData tileData = null;
        if (!TryGetTileData(out tileData))
        {
            if (hideWhenTileDataMissing)
            {
                return;
            }
        }

        if (!EnsureSharedTooltip())
        {
            return;
        }

        if (activeOwner != null && activeOwner != this)
        {
            activeOwner.isHovering = false;
        }

        activeOwner = this;
        isHovering = true;

        string mainText = tileData != null ? tileData.GetTileDisplayName() : "Unknown Tile";
        string faceText = BuildFaceValueText(tileData, out bool showFaceTooltip);
        EditionVisualData editionData = BuildEditionData(tileData, out bool showEditionTooltip);

        ApplyPanelVisualSettings(sharedMainPanel, mainTooltipStyle, tooltipCornerRadius);
        sharedMainPanel.Text.text = mainText;
        ResizePanelToFitText(sharedMainPanel, mainTooltipStyle);

        ConfigureSubTooltip(sharedFacePanel, faceValueTooltip, faceText, showFaceTooltip);
        ConfigureEditionSubTooltip(sharedEditionPanel, editionTooltip, editionData, showEditionTooltip);

        currentShowsEdition = showEditionTooltip;
        LayoutSubTooltipPanels(showFaceTooltip, showEditionTooltip);
        UpdateConnector(sharedFaceConnectorRect, sharedFaceConnectorImage, showFaceTooltip, faceValueTooltip, sharedFacePanel.PanelRect);
        UpdateConnector(sharedEditionConnectorRect, sharedEditionConnectorImage, showEditionTooltip, editionTooltip, sharedEditionPanel.PanelRect);

        sharedRootRect.gameObject.SetActive(true);
        sharedRootRect.SetAsLastSibling();
        UpdateTooltipPosition();
    }

    private void StopHoverAndHideIfOwner()
    {
        isHovering = false;

        if (activeOwner != this)
        {
            return;
        }

        activeOwner = null;
        if (sharedRootRect != null)
        {
            sharedRootRect.gameObject.SetActive(false);
        }
    }

    private void EnsureSettingsInitialized()
    {
        if (mainTooltipStyle == null)
        {
            mainTooltipStyle = new PanelStyle();
        }

        if (faceValueTooltip == null)
        {
            faceValueTooltip = CreateDefaultFaceValueStyle();
        }

        if (editionTooltip == null)
        {
            editionTooltip = CreateDefaultEditionStyle();
        }

        if (editionDescriptions == null)
        {
            editionDescriptions = new List<EditionDescriptionEntry>();
        }
    }

    private void ClampAllSettings()
    {
        if (mainTooltipStyle != null)
        {
            mainTooltipStyle.ClampValues();
        }

        if (faceValueTooltip != null)
        {
            faceValueTooltip.ClampValues();
        }

        if (editionTooltip != null)
        {
            editionTooltip.ClampValues();
        }

        horizontalEdgePadding = Mathf.Max(0f, horizontalEdgePadding);
        subTooltipVerticalGap = Mathf.Max(0f, subTooltipVerticalGap);
        tooltipCornerRadius = Mathf.Max(0f, tooltipCornerRadius);
        editionNameFontMultiplier = Mathf.Max(0.1f, editionNameFontMultiplier);
        editionNameBadgePadding.x = Mathf.Max(0f, editionNameBadgePadding.x);
        editionNameBadgePadding.y = Mathf.Max(0f, editionNameBadgePadding.y);
        editionNameDescriptionGap = Mathf.Max(0f, editionNameDescriptionGap);
    }

    private static SubTooltipStyle CreateDefaultFaceValueStyle()
    {
        var style = new SubTooltipStyle
        {
            offsetFromMain = new Vector2(0f, -66f),
            connectorStartOffset = new Vector2(0f, -26f),
            connectorEndOffset = new Vector2(0f, 22f)
        };
        style.panelStyle.panelSize = new Vector2(220f, 46f);
        style.panelStyle.fontSize = 20f;
        return style;
    }

    private static SubTooltipStyle CreateDefaultEditionStyle()
    {
        var style = new SubTooltipStyle
        {
            offsetFromMain = new Vector2(0f, -122f),
            connectorStartOffset = new Vector2(0f, -26f),
            connectorEndOffset = new Vector2(0f, 22f)
        };
        style.panelStyle.panelSize = new Vector2(300f, 58f);
        style.panelStyle.fontSize = 18f;
        style.panelStyle.textAlignment = TextAlignmentOptions.Left;
        return style;
    }

    private bool TryGetTileData(out MahjongTileData tileData)
    {
        tileData = null;

        if (tileHolder == null)
        {
            tileHolder = GetComponent<MahjongTileHolder>();
        }

        if (tileHolder == null || tileHolder.TileData == null)
        {
            if (!hasLoggedMissingData)
            {
                Debug.LogWarning($"Tooltip on '{name}' could not find MahjongTileHolder/TileData.", this);
                hasLoggedMissingData = true;
            }

            return false;
        }

        tileData = tileHolder.TileData;
        return true;
    }

    private string BuildFaceValueText(MahjongTileData tileData, out bool shouldShow)
    {
        shouldShow = faceValueTooltip != null && faceValueTooltip.isEnabled && tileData != null;
        if (!shouldShow)
        {
            return string.Empty;
        }

        int faceValue = ResolveFaceValue(tileData);
        string fallback = $"+{faceValue} Points";
        return SafeFormat(faceValueTextFormat, fallback, faceValue);
    }

    private Canvas ResolveCanvas()
    {
        if (targetCanvas != null)
        {
            return targetCanvas;
        }

        GameObject canvasObject = GameObject.FindWithTag("UICanvas");
        if (canvasObject != null)
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }
        }

        Debug.LogWarning($"Tooltip on '{name}' could not find a Canvas tagged 'UICanvas'.", this);
        return null;
    }

    private Camera ResolveWorldCamera()
    {
        if (worldCameraOverride != null)
        {
            return worldCameraOverride;
        }

        GameObject cameraObject = GameObject.FindWithTag("MainCamera");
        if (cameraObject != null)
        {
            Camera cam = cameraObject.GetComponent<Camera>();
            if (cam != null)
            {
                return cam;
            }
        }

        Debug.LogWarning($"Tooltip on '{name}' could not find a Camera tagged 'MainCamera'.", this);
        return null;
    }

    private void UpdateTooltipPosition()
    {
        if (sharedRootRect == null || sharedCanvas == null)
        {
            return;
        }

        Camera worldCamera = ResolveWorldCamera();
        if (worldCamera == null)
        {
            return;
        }

        Vector3 worldPosition = transform.position + worldOffset;
        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0f)
        {
            sharedRootRect.gameObject.SetActive(false);
            return;
        }

        if (!sharedRootRect.gameObject.activeSelf)
        {
            sharedRootRect.gameObject.SetActive(true);
        }

        RectTransform canvasRect = sharedCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Camera uiCamera = sharedCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sharedCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
        {
            Vector2 totalOffset = screenOffset + (currentShowsEdition ? editionActiveScreenOffset : Vector2.zero);
            Vector2 targetAnchoredPosition = localPoint + totalOffset;
            sharedRootRect.anchoredPosition = ClampRootAnchoredPositionX(targetAnchoredPosition, canvasRect);
        }
    }

    private Vector2 ClampRootAnchoredPositionX(Vector2 anchoredPosition, RectTransform canvasRect)
    {
        GetVisiblePanelBoundsX(out float minLocalX, out float maxLocalX);

        float minX = canvasRect.rect.xMin + horizontalEdgePadding - minLocalX;
        float maxX = canvasRect.rect.xMax - horizontalEdgePadding - maxLocalX;

        if (minX > maxX)
        {
            anchoredPosition.x = 0f;
            return anchoredPosition;
        }

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        return anchoredPosition;
    }

    private static void GetVisiblePanelBoundsX(out float minLocalX, out float maxLocalX)
    {
        minLocalX = float.PositiveInfinity;
        maxLocalX = float.NegativeInfinity;

        IncludePanelBoundsX(sharedMainPanel, ref minLocalX, ref maxLocalX);
        IncludePanelBoundsX(sharedFacePanel, ref minLocalX, ref maxLocalX);
        IncludePanelBoundsX(sharedEditionPanel, ref minLocalX, ref maxLocalX);

        if (float.IsInfinity(minLocalX) || float.IsInfinity(maxLocalX))
        {
            minLocalX = -110f;
            maxLocalX = 110f;
        }
    }

    private static void IncludePanelBoundsX(TooltipPanelRefs panelRefs, ref float minLocalX, ref float maxLocalX)
    {
        if (panelRefs == null || panelRefs.PanelRect == null || !panelRefs.PanelRect.gameObject.activeSelf)
        {
            return;
        }

        float halfWidth = panelRefs.PanelRect.rect.width * 0.5f;
        float left = panelRefs.PanelRect.anchoredPosition.x - halfWidth;
        float right = panelRefs.PanelRect.anchoredPosition.x + halfWidth;

        if (left < minLocalX)
        {
            minLocalX = left;
        }

        if (right > maxLocalX)
        {
            maxLocalX = right;
        }
    }
}