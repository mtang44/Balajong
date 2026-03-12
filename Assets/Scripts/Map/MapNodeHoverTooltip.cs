using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapNodeHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.12f;
    [SerializeField, Min(0f)] private float hoverTransitionSpeed = 12f;
    [SerializeField, FormerlySerializedAs("hoverGlowColor")] private Color hoverBorderColor = new Color(1f, 0.95f, 0.65f, 0.9f);
    [SerializeField, FormerlySerializedAs("hoverGlowDistance")] private Vector2 hoverBorderThickness = new Vector2(6f, 6f);
    [SerializeField] private Outline hoverOutline;

    [Header("Tooltip")]
    [SerializeField] private bool showTooltipOnHover = true;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 92f);
    [SerializeField] private Color tooltipBackgroundColor = new Color(0.07f, 0.11f, 0.2f, 0.92f);
    [SerializeField] private Color tooltipTextColor = Color.white;
    [SerializeField, Min(120f)] private float tooltipMinWidth = 210f;
    [SerializeField, Min(1)] private int tooltipFontSize = 18;
    [SerializeField, Min(0)] private int tooltipPadding = 10;
    [SerializeField, Min(0f)] private float tooltipSpacing = 4f;
    [SerializeField] private Font tooltipFont;
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private Text tooltipTypeText;
    [SerializeField] private Text tooltipHealthText;

    private Image nodeImage;
    private Button nodeButton;
    private MapNodeType nodeType;
    private Vector3 baseScale = Vector3.one;
    private bool isPointerOver;
    private bool canHover = true;
    private float hoverAmount;
    private bool isInitialized;
    private string enemyHealthText = DefaultEnemyHealthText;

    private static Font cachedTooltipFont;
    private const string DefaultEnemyHealthText = "???";

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float targetAmount = isPointerOver && canHover ? 1f : 0f;
        if (hoverTransitionSpeed <= 0f)
        {
            hoverAmount = targetAmount;
        }
        else
        {
            hoverAmount = Mathf.MoveTowards(hoverAmount, targetAmount, hoverTransitionSpeed * Time.unscaledDeltaTime);
        }

        ApplyHoverVisuals();
    }

    public void Setup(Image image, Button button, MapNodeType type)
    {
        nodeImage = image != null ? image : GetComponentInChildren<Image>();
        nodeButton = button != null ? button : GetComponent<Button>();
        nodeType = type;

        baseScale = transform.localScale;
        isPointerOver = false;
        hoverAmount = 0f;

        EnsureHoverOutline();
        EnsureTooltip();
        RefreshTooltipContent();
        SetTooltipVisible(false);
        ApplyHoverVisuals();

        isInitialized = true;
    }

    public void SetEnemyHealthText(string newText)
    {
        enemyHealthText = string.IsNullOrWhiteSpace(newText) ? DefaultEnemyHealthText : newText;
        RefreshTooltipContent();
    }

    public void SetHoverEnabled(bool enabled)
    {
        canHover = enabled;
        if (canHover)
        {
            return;
        }

        isPointerOver = false;
        hoverAmount = 0f;
        SetTooltipVisible(false);
        ApplyHoverVisuals();
    }

    public void HideTooltip()
    {
        isPointerOver = false;
        SetTooltipVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canHover)
        {
            return;
        }

        isPointerOver = true;
        SetTooltipVisible(showTooltipOnHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        SetTooltipVisible(false);
    }

    private void OnMouseEnter()
    {
        if (nodeButton != null || !canHover)
        {
            return;
        }

        isPointerOver = true;
        SetTooltipVisible(showTooltipOnHover);
    }

    private void OnMouseExit()
    {
        if (nodeButton != null)
        {
            return;
        }

        isPointerOver = false;
        SetTooltipVisible(false);
    }

    private void OnDisable()
    {
        isPointerOver = false;
        hoverAmount = 0f;
        SetTooltipVisible(false);
        ApplyHoverVisuals();
    }

    private void EnsureHoverOutline()
    {
        if (nodeImage == null)
        {
            return;
        }

        if (hoverOutline == null)
        {
            hoverOutline = nodeImage.GetComponent<Outline>();
        }

        if (hoverOutline == null)
        {
            hoverOutline = nodeImage.gameObject.AddComponent<Outline>();
        }

        hoverOutline.useGraphicAlpha = true;
    }

    private void EnsureTooltip()
    {
        if (!showTooltipOnHover)
        {
            SetTooltipVisible(false);
            return;
        }

        if (tooltipRoot == null)
        {
            GameObject tooltipObject = new GameObject("Tooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            tooltipObject.transform.SetParent(transform, false);
            tooltipRoot = tooltipObject.GetComponent<RectTransform>();
        }

        Image background = tooltipRoot.GetComponent<Image>();
        if (background == null)
        {
            background = tooltipRoot.gameObject.AddComponent<Image>();
        }

        background.raycastTarget = false;
        background.color = tooltipBackgroundColor;

        VerticalLayoutGroup layout = tooltipRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = tooltipRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(tooltipPadding, tooltipPadding, tooltipPadding, tooltipPadding);
        layout.spacing = tooltipSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = tooltipRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRoot.pivot = new Vector2(0.5f, 0f);
        tooltipRoot.anchoredPosition = tooltipOffset;
        tooltipRoot.localScale = Vector3.one;

        if (tooltipTypeText == null)
        {
            tooltipTypeText = CreateTooltipText("TypeText");
        }

        if (tooltipHealthText == null)
        {
            tooltipHealthText = CreateTooltipText("EnemyHealthText");
        }

        ApplyTooltipTextStyle(tooltipTypeText);
        ApplyTooltipTextStyle(tooltipHealthText);
        SetTooltipVisible(false);
    }

    private Text CreateTooltipText(string objectName)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textObject.transform.SetParent(tooltipRoot, false);

        Text text = textObject.GetComponent<Text>();
        text.raycastTarget = false;

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = tooltipMinWidth;

        return text;
    }

    private void ApplyTooltipTextStyle(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.font = ResolveTooltipFont();
        text.fontSize = tooltipFontSize;
        text.color = tooltipTextColor;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.minWidth = tooltipMinWidth;
        }
    }

    private void RefreshTooltipContent()
    {
        if (tooltipTypeText == null || tooltipHealthText == null)
        {
            return;
        }

        tooltipTypeText.text = $"{GetNodeTypeDisplayName(nodeType)}";
        tooltipHealthText.text = $"Enemy Health: {enemyHealthText}";
    }

    private void SetTooltipVisible(bool visible)
    {
        if (tooltipRoot == null)
        {
            return;
        }

        bool shouldShow = visible && canHover && showTooltipOnHover;
        tooltipRoot.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            tooltipRoot.SetAsLastSibling();
        }
    }

    private static string GetNodeTypeDisplayName(MapNodeType type)
    {
        return type switch
        {
            MapNodeType.Battle => "Normal Battle",
            MapNodeType.Elite => "Elite Battle",
            MapNodeType.Boss => "Boss Battle",
            MapNodeType.Start => "Start",
            _ => type.ToString()
        };
    }

    private Font ResolveTooltipFont()
    {
        if (tooltipFont != null)
        {
            return tooltipFont;
        }

        if (cachedTooltipFont != null)
        {
            return cachedTooltipFont;
        }

        cachedTooltipFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedTooltipFont == null)
        {
            cachedTooltipFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return cachedTooltipFont;
    }

    private void ApplyHoverVisuals()
    {
        float easedAmount = 1f - Mathf.Pow(1f - hoverAmount, 3f);
        float targetScale = Mathf.Lerp(1f, Mathf.Max(1f, hoverScaleMultiplier), easedAmount);
        transform.localScale = baseScale * targetScale;

        if (hoverOutline == null)
        {
            return;
        }

        bool showOutline = easedAmount > 0.001f;
        hoverOutline.enabled = showOutline;
        if (showOutline)
        {
            hoverOutline.effectColor = hoverBorderColor;
            hoverOutline.effectDistance = hoverBorderThickness;
        }
    }
}
