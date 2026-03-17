using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class JokerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	private RectTransform rectTransform;
	private RectTransform parentRect;
	private HorizontalLayoutGroup parentLayoutGroup;
	private CanvasGroup canvasGroup;

	private readonly List<RectTransform> siblingRects = new List<RectTransform>();
	private readonly List<float> slotPositions = new List<float>();

	private bool isDragging;
	private bool layoutGroupWasEnabled;
	private bool originalBlocksRaycasts;
	private float originalAlpha;
	private Vector3 originalLocalPosition;
	private int originalSiblingIndex;
	private int currentPreviewIndex;
	private Camera dragEventCamera;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
			return;

		if (rectTransform == null)
			rectTransform = GetComponent<RectTransform>();

		parentRect = rectTransform.parent as RectTransform;
		if (parentRect == null)
			return;

		parentLayoutGroup = parentRect.GetComponent<HorizontalLayoutGroup>();
		CaptureSiblingOrderAndSlots();

		originalLocalPosition = rectTransform.localPosition;
		originalSiblingIndex = transform.GetSiblingIndex();
		currentPreviewIndex = originalSiblingIndex;

		dragEventCamera = eventData.pressEventCamera;
		layoutGroupWasEnabled = parentLayoutGroup != null && parentLayoutGroup.enabled;
		if (layoutGroupWasEnabled)
		{
			parentLayoutGroup.enabled = false;
		}

		originalBlocksRaycasts = canvasGroup.blocksRaycasts;
		originalAlpha = canvasGroup.alpha;
		canvasGroup.blocksRaycasts = false;
		canvasGroup.alpha = 0.9f;

		isDragging = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!isDragging || eventData == null)
			return;

		if (parentRect == null)
			return;

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, dragEventCamera, out Vector2 localPoint))
		{
			rectTransform.localPosition = new Vector3(localPoint.x, originalLocalPosition.y, originalLocalPosition.z);
		}

		int previewIndex = CalculateInsertionIndex();
		if (previewIndex != currentPreviewIndex)
		{
			currentPreviewIndex = previewIndex;
			RepositionSiblingsWithGap(currentPreviewIndex);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!isDragging)
			return;

		CompleteDragAndSync(applyReorder: true);
	}

	private void OnDisable()
	{
		if (!isDragging)
			return;

		CompleteDragAndSync(applyReorder: false);
	}

	private void CaptureSiblingOrderAndSlots()
	{
		siblingRects.Clear();
		slotPositions.Clear();

		if (parentRect == null)
			return;

		for (int i = 0; i < parentRect.childCount; i++)
		{
			RectTransform childRect = parentRect.GetChild(i) as RectTransform;
			if (childRect == null)
				continue;

			siblingRects.Add(childRect);
			slotPositions.Add(childRect.localPosition.x);
		}
	}

	private int CalculateInsertionIndex()
	{
		if (slotPositions.Count == 0)
			return 0;

		float localX = rectTransform.localPosition.x;
		int closestIndex = 0;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < slotPositions.Count; i++)
		{
			float distance = Mathf.Abs(localX - slotPositions[i]);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestIndex = i;
			}
		}

		return closestIndex;
	}

	private void RepositionSiblingsWithGap(int gapIndex)
	{
		if (siblingRects.Count == 0 || slotPositions.Count == 0)
			return;

		List<RectTransform> otherJokers = new List<RectTransform>();
		for (int i = 0; i < siblingRects.Count; i++)
		{
			RectTransform siblingRect = siblingRects[i];
			if (siblingRect == null || siblingRect == rectTransform)
				continue;

			otherJokers.Add(siblingRect);
		}

		for (int i = 0; i < otherJokers.Count; i++)
		{
			RectTransform siblingRect = otherJokers[i];
			int visualIndex = i >= gapIndex ? i + 1 : i;
			visualIndex = Mathf.Clamp(visualIndex, 0, slotPositions.Count - 1);
			Vector3 siblingLocalPosition = siblingRect.localPosition;
			siblingRect.localPosition = new Vector3(slotPositions[visualIndex], siblingLocalPosition.y, siblingLocalPosition.z);
		}
	}

	private void CompleteDragAndSync(bool applyReorder)
	{
		isDragging = false;

		if (parentRect == null)
			return;

		if (applyReorder && currentPreviewIndex >= 0)
		{
			rectTransform.SetSiblingIndex(Mathf.Clamp(currentPreviewIndex, 0, parentRect.childCount - 1));
		}
		else
		{
			rectTransform.localPosition = originalLocalPosition;
			rectTransform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, parentRect.childCount - 1));
		}

		if (layoutGroupWasEnabled && parentLayoutGroup != null)
			parentLayoutGroup.enabled = true;

		canvasGroup.blocksRaycasts = originalBlocksRaycasts;
		canvasGroup.alpha = originalAlpha;

		LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
		Canvas.ForceUpdateCanvases();
		JokerManager.Instance?.SyncJokerOrderFromUI();

		siblingRects.Clear();
		slotPositions.Clear();
	}
}
