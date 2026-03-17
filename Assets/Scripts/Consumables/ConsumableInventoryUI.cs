using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// Backend hook-up for consumable inventory in RackingScene. Teammates add responsive UI (item icons, layout).
// - Exactly 2 slots (index 0 and 1). Empty slots show no item; click selects the consumable in that slot.
// - ConsumableManager.SelectionChanged / InventoryChanged drive refresh; slot clicks call ConsumableManager.Select(index).
// - Use button (ConsumableEffectSystem) becomes available when an item is selected; correct tile count enables click.

public class ConsumableInventoryUI : MonoBehaviour
{
    [SerializeField] private List<Button> slotButtons = new List<Button>();
    [Tooltip("Optional: one per slot (2), to show selected state.")]
    [SerializeField] private List<Image> slotHighlights = new List<Image>();
    [Tooltip("Optional: one per slot (2), to show item name. Empty slot can show blank or placeholder.")]
    [SerializeField] private List<TMP_Text> slotLabels = new List<TMP_Text>();
    [SerializeField] private List<RawImage> imagePanels = new List<RawImage>();
    [SerializeField] private List<Texture> consumableImageArray = new  List<Texture>();
 

    private void Start()
    {
        if (ConsumableManager.Instance == null) return;

        ConsumableManager.Instance.InventoryChanged += Refresh;
        ConsumableManager.Instance.SelectionChanged += OnSelectionChanged;

        for (int i = 0; i < ConsumableManager.InventorySize; i++)
        {
            int index = i;
            if (i < slotButtons.Count && slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => ConsumableManager.Instance?.Select(index));

        }

        Refresh();
        OnSelectionChanged(ConsumableManager.Instance.SelectedIndex);
    }

    private void OnDestroy()
    {
        if (ConsumableManager.Instance == null) return;
        ConsumableManager.Instance.InventoryChanged -= Refresh;
        ConsumableManager.Instance.SelectionChanged -= OnSelectionChanged;
    }

    private void Refresh()
    {
        var manager = ConsumableManager.Instance;
        if (manager == null) return;

        for (int i = 0; i < ConsumableManager.InventorySize; i++)
        {
            var c = manager.GetAt(i);
            bool hasItem = c != null;

            if (i < slotButtons.Count && slotButtons[i] != null)
            {
                //slotButtons[i].gameObject.SetActive(true);
                slotButtons[i].interactable = hasItem;
            }

            if (i < slotLabels.Count && slotLabels[i] != null)
                slotLabels[i].text = hasItem ? c.name : "";
                imagePanels[i].texture = hasItem? consumableImageArray[c.imageIndex]: null;

        }
    }

    private void OnSelectionChanged(int selectedIndex)
    {
        
        for (int i = 0; i < slotHighlights.Count; i++)
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].gameObject.SetActive(i == selectedIndex);
        }
    }
}
