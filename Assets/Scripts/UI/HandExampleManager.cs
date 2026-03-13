using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HandExampleManager : MonoBehaviour
{
	[System.Serializable]
	public struct HandExampleEntry
	{
		public TMP_Text handText;
		public GameObject exampleObject;
	}

	[Header("Hand Mappings")]
	[SerializeField] private List<HandExampleEntry> handExamples = new List<HandExampleEntry>();

	[Header("Behavior")]
	[SerializeField] private bool hideAllOnStart = true;
	[SerializeField] private bool hideAllOnDisable = true;
	[SerializeField] private bool forceTextRaycastTarget = true;
	[SerializeField] private GameObject noSelectionTextObject;

	[Header("Text Colors")]
	[SerializeField] private Color defaultTextColor = Color.white;
	[SerializeField] private Color hoverTextColor = new Color(1f, 0.95f, 0.7f, 1f);
	[SerializeField] private Color selectedTextColor = new Color(1f, 0.8f, 0.2f, 1f);

	private int selectedIndex = -1;

	private void LateUpdate()
	{
		if (!IsValidIndex(selectedIndex))
		{
			return;
		}

		TMP_Text selectedText = handExamples[selectedIndex].handText;
		if (selectedText == null || !selectedText.gameObject.activeInHierarchy)
		{
			ClearSelection(hideExamples: true);
		}
	}

	private void Awake()
	{
		BindHoverTargets();
		selectedIndex = -1;
		ApplyTextColors();
		UpdateNoSelectionTextVisibility();
		if (hideAllOnStart)
		{
			HideAllExamples();
		}
	}

	private void OnDisable()
	{
		selectedIndex = -1;
		ApplyTextColors();
		UpdateNoSelectionTextVisibility();

		if (hideAllOnDisable)
		{
			HideAllExamples();
		}
	}

	[ContextMenu("Bind Pointer Targets")]
	public void BindHoverTargets()
	{
		for (int i = 0; i < handExamples.Count; i++)
		{
			TMP_Text text = handExamples[i].handText;
			if (text == null)
			{
				continue;
			}

			if (forceTextRaycastTarget)
			{
				text.raycastTarget = true;
			}

			HandExampleHoverTarget hoverTarget = text.GetComponent<HandExampleHoverTarget>();
			if (hoverTarget == null)
			{
				hoverTarget = text.gameObject.AddComponent<HandExampleHoverTarget>();
			}

			hoverTarget.Configure(this, i);
		}
	}

	public void HandlePointerEnter(int index)
	{
		if (!IsEntryInteractive(index))
		{
			return;
		}

		if (index != selectedIndex)
		{
			SetTextColor(index, hoverTextColor);
		}
 	}

	public void HandlePointerExit(int index)
	{
		if (!IsEntryInteractive(index))
		{
			return;
		}

		if (index != selectedIndex)
		{
			SetTextColor(index, defaultTextColor);
		}
	}

	public void HandlePointerClick(int index)
	{
		if (!IsEntryInteractive(index))
		{
			return;
		}

		selectedIndex = index;
		ShowOnlyExample(index);
		ApplyTextColors();
		UpdateNoSelectionTextVisibility();
	}

	public void HideAllExamples()
	{
		for (int i = 0; i < handExamples.Count; i++)
		{
			GameObject exampleObject = handExamples[i].exampleObject;
			if (exampleObject != null)
			{
				exampleObject.SetActive(false);
			}
		}
	}

	private void ShowOnlyExample(int selectedExampleIndex)
	{
		for (int i = 0; i < handExamples.Count; i++)
		{
			GameObject exampleObject = handExamples[i].exampleObject;
			if (exampleObject != null)
			{
				exampleObject.SetActive(i == selectedExampleIndex);
			}
		}
	}

	private void ClearSelection(bool hideExamples)
	{
		selectedIndex = -1;
		ApplyTextColors();
		UpdateNoSelectionTextVisibility();
		if (hideExamples)
		{
			HideAllExamples();
		}
	}

	private void UpdateNoSelectionTextVisibility()
	{
		if (noSelectionTextObject == null)
		{
			return;
		}

		bool showNoSelectionText = selectedIndex < 0;
		if (noSelectionTextObject.activeSelf != showNoSelectionText)
		{
			noSelectionTextObject.SetActive(showNoSelectionText);
		}
	}

	private void ApplyTextColors()
	{
		for (int i = 0; i < handExamples.Count; i++)
		{
			if (!IsValidIndex(i))
			{
				continue;
			}

			Color targetColor = i == selectedIndex ? selectedTextColor : defaultTextColor;
			SetTextColor(i, targetColor);
		}
	}

	private void SetTextColor(int index, Color color)
	{
		if (!IsValidIndex(index))
		{
			return;
		}

		TMP_Text text = handExamples[index].handText;
		if (text != null)
		{
			text.color = color;
		}
	}

	private bool IsValidIndex(int index)
	{
		return index >= 0 && index < handExamples.Count;
	}

	private bool IsEntryInteractive(int index)
	{
		if (!IsValidIndex(index))
		{
			return false;
		}

		TMP_Text text = handExamples[index].handText;
		return text != null && text.gameObject.activeInHierarchy;
	}
}
