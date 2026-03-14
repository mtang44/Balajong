using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Header("Tile Data")]
	[SerializeField] private MahjongTileHolder tileHolder;
	[SerializeField] private bool hideWhenTileDataMissing = true;

	[Header("Canvas")]
	[SerializeField] private Canvas targetCanvas;
	[SerializeField] private Camera worldCameraOverride;

	[Header("Position")]
	[SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
	[SerializeField] private Vector2 screenOffset = Vector2.zero;
	[SerializeField, Min(0f)] private float horizontalEdgePadding = 16f;

	[Header("Size")]
	[SerializeField] private Vector2 panelSize = new Vector2(220f, 52f);
	[SerializeField] private Vector2 panelPadding = new Vector2(12f, 8f);

	[Header("Text")]
	[SerializeField] private TMP_FontAsset fontAsset;
	[SerializeField, Min(1f)] private float fontSize = 24f;
	[SerializeField] private FontStyles fontStyle = FontStyles.Bold;
	[SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
	[SerializeField] private Color textColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
	[SerializeField] private bool autoSizeText = false;
	[SerializeField, Min(1f)] private float autoSizeMin = 14f;
	[SerializeField, Min(1f)] private float autoSizeMax = 32f;

	[Header("Colors")]
	[SerializeField] private Color backgroundColor = Color.white;
	[SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.35f);
	[SerializeField, Min(0f)] private float borderThickness = 1f;

	[Header("Fallback")]
	[SerializeField] private bool useMouseHoverFallback = false;

	private bool isHovering;
	private bool hasLoggedMissingData;

	private static RectTransform sharedPanelRect;
	private static RectTransform sharedTextRect;
	private static Image sharedPanelImage;
	private static Outline sharedPanelOutline;
	private static TextMeshProUGUI sharedText;
	private static Canvas sharedCanvas;
	private static Tooltip activeOwner;

	private void Awake()
	{
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
		autoSizeMin = Mathf.Max(1f, autoSizeMin);
		autoSizeMax = Mathf.Max(1f, autoSizeMax);
		panelSize.x = Mathf.Max(1f, panelSize.x);
		panelSize.y = Mathf.Max(1f, panelSize.y);
		panelPadding.x = Mathf.Max(0f, panelPadding.x);
		panelPadding.y = Mathf.Max(0f, panelPadding.y);
		horizontalEdgePadding = Mathf.Max(0f, horizontalEdgePadding);

		if (activeOwner == this && sharedPanelRect != null)
		{
			ApplyVisualSettings();
			UpdateTooltipPosition();
		}
	}

	private void ShowTooltip()
	{
		if (!TryGetTileDisplayName(out string displayName))
		{
			if (hideWhenTileDataMissing)
			{
				return;
			}

			displayName = "Unknown Tile";
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

		ApplyVisualSettings();
		sharedText.text = displayName;
		sharedPanelRect.gameObject.SetActive(true);
		sharedPanelRect.SetAsLastSibling();
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
		if (sharedPanelRect != null)
		{
			sharedPanelRect.gameObject.SetActive(false);
		}
	}

	private bool TryGetTileDisplayName(out string displayName)
	{
		displayName = "Unknown Tile";

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

		displayName = tileHolder.TileData.GetTileDisplayName();
		return true;
	}

	private bool EnsureSharedTooltip()
	{
		Canvas canvas = ResolveCanvas();
		if (canvas == null)
		{
			return false;
		}

		if (sharedPanelRect != null && sharedCanvas != canvas)
		{
			Destroy(sharedPanelRect.gameObject);
			sharedPanelRect = null;
			sharedTextRect = null;
			sharedPanelImage = null;
			sharedPanelOutline = null;
			sharedText = null;
			sharedCanvas = null;
		}

		if (sharedPanelRect != null)
		{
			return true;
		}

		sharedCanvas = canvas;

		GameObject panelObject = new GameObject("TileTooltip", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
		panelObject.layer = canvas.gameObject.layer;

		sharedPanelRect = panelObject.GetComponent<RectTransform>();
		sharedPanelRect.SetParent(canvas.transform, false);
		sharedPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
		sharedPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
		sharedPanelRect.pivot = new Vector2(0.5f, 0.5f);

		sharedPanelImage = panelObject.GetComponent<Image>();
		sharedPanelImage.raycastTarget = false;

		sharedPanelOutline = panelObject.GetComponent<Outline>();
		sharedPanelOutline.useGraphicAlpha = true;

		CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
		group.interactable = false;
		group.blocksRaycasts = false;

		GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
		textObject.layer = panelObject.layer;

		sharedTextRect = textObject.GetComponent<RectTransform>();
		sharedTextRect.SetParent(sharedPanelRect, false);
		sharedTextRect.anchorMin = Vector2.zero;
		sharedTextRect.anchorMax = Vector2.one;

		sharedText = textObject.GetComponent<TextMeshProUGUI>();
		sharedText.raycastTarget = false;
		sharedText.textWrappingMode = TextWrappingModes.NoWrap;

		sharedPanelRect.gameObject.SetActive(false);
		return true;
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

	private void ApplyVisualSettings()
	{
		if (sharedPanelRect == null || sharedTextRect == null || sharedPanelImage == null || sharedText == null)
		{
			return;
		}

		sharedPanelRect.sizeDelta = panelSize;
		sharedTextRect.offsetMin = new Vector2(panelPadding.x, panelPadding.y);
		sharedTextRect.offsetMax = new Vector2(-panelPadding.x, -panelPadding.y);

		sharedPanelImage.color = backgroundColor;

		if (sharedPanelOutline != null)
		{
			bool drawBorder = borderThickness > 0f;
			sharedPanelOutline.enabled = drawBorder;
			if (drawBorder)
			{
				sharedPanelOutline.effectColor = borderColor;
				sharedPanelOutline.effectDistance = new Vector2(borderThickness, borderThickness);
			}
		}

		if (fontAsset != null)
		{
			sharedText.font = fontAsset;
		}

		sharedText.fontSize = fontSize;
		sharedText.fontStyle = fontStyle;
		sharedText.alignment = textAlignment;
		sharedText.color = textColor;
		sharedText.enableAutoSizing = autoSizeText;

		if (autoSizeText)
		{
			float minSize = Mathf.Min(autoSizeMin, autoSizeMax);
			float maxSize = Mathf.Max(autoSizeMin, autoSizeMax);
			sharedText.fontSizeMin = minSize;
			sharedText.fontSizeMax = maxSize;
		}
	}

	private void UpdateTooltipPosition()
	{
		if (sharedPanelRect == null || sharedCanvas == null)
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
			sharedPanelRect.gameObject.SetActive(false);
			return;
		}

		if (!sharedPanelRect.gameObject.activeSelf)
		{
			sharedPanelRect.gameObject.SetActive(true);
		}

		RectTransform canvasRect = sharedCanvas.transform as RectTransform;
		if (canvasRect == null)
		{
			return;
		}

		Camera uiCamera = sharedCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sharedCanvas.worldCamera;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
		{
			Vector2 targetAnchoredPosition = localPoint + screenOffset;
			sharedPanelRect.anchoredPosition = ClampAnchoredPositionX(targetAnchoredPosition, canvasRect);
		}
	}

	private Vector2 ClampAnchoredPositionX(Vector2 anchoredPosition, RectTransform canvasRect)
	{
		float halfWidth = sharedPanelRect.rect.width * 0.5f;
		float minX = canvasRect.rect.xMin + halfWidth + horizontalEdgePadding;
		float maxX = canvasRect.rect.xMax - halfWidth - horizontalEdgePadding;

		if (minX > maxX)
		{
			anchoredPosition.x = 0f;
			return anchoredPosition;
		}

		anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
		return anchoredPosition;
	}
}
