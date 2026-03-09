using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// This script is attached to the MahjongTile GameObject. It handles collision interaction, and will add itself to the DeckManager on click
public class TileSelect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    DeckManager deckManager;
    public MahjongTileData tileData;

    [SerializeField] private float tooltipHeightOffset = 1.5f;

    GameObject tooltip; // Reference to the tooltip GameObject
    private bool isHoveringOverTile = false;
    private Vector3 originalPosition;
    private int originalIndex;
    private int currentPreviewIndex;
    private bool isDragging = false;
    private float dragStartTime;
    private float originalZOffset = 0.5f; // How far forward to lift the tile

    void Start()
    {
        deckManager = DeckManager.Instance;
        MahjongTileHolder holder = GetComponent<MahjongTileHolder>();
        if (holder != null)
        {
            tileData = holder.TileData;
        }
        else
        {
            Debug.LogError("MahjongTileHolder component not found on " + gameObject.name);
        }
        tooltip = GameObject.FindGameObjectWithTag("Tooltip");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Send in the tooltip
        if (tooltip != null && tileData != null)
        {
            // Make sure tooltip is active
            GameObject tooltipButton = tooltip.transform.GetChild(0).gameObject;
            if (!tooltipButton.activeSelf)
            {
                tooltipButton.SetActive(true);
            }
            
            TextMeshProUGUI tooltipText = tooltip.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (tooltipText != null)
            {
                string displayName = tileData.GetTileDisplayName();
                tooltipText.text = displayName;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in tooltip!");
            }
            
            // Position tooltip above the tile
            UpdateTooltipPosition();
        }
        isHoveringOverTile = true;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        //Quiet the tooltip
        isHoveringOverTile = false;
        if (tooltip != null && tooltip.transform.childCount > 0)
        {
            tooltip.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltip != null && tooltip.transform.childCount > 0)
        {
            GameObject tooltipChild = tooltip.transform.GetChild(0).gameObject;
            if (!tooltipChild.activeSelf) return;
            
            RectTransform tooltipRect = tooltipChild.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                Canvas canvas = tooltip.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // Get the tile's position in world space
                    Vector3 tileWorldPos = transform.position;
                    
                    // Add offset above the tile (accounting for tile height + extra space)
                    Vector3 tooltipWorldPos = tileWorldPos + new Vector3(0f, tooltipHeightOffset, 0f);
                    
                    // Convert to screen space
                    Vector3 screenPos = canvas.worldCamera.WorldToScreenPoint(tooltipWorldPos);
                    
                    // Convert screen space to canvas position
                    Vector3 canvasScreenPoint = new Vector3(screenPos.x, screenPos.y, canvas.planeDistance);
                    Vector3 canvasWorldPoint = canvas.worldCamera.ScreenToWorldPoint(canvasScreenPoint);
                    
                    tooltipRect.position = canvasWorldPoint;
                }
            }
        }
    }

    // Pick the tile up - begin drag
    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartTime = Time.time;
    }

    // Drop the tile - end drag
    public void OnPointerUp(PointerEventData eventData)
    {
        // If we didn't drag (quick click), process as selection
        if (!isDragging && Time.time - dragStartTime < 0.2f)
        {
            clicked();
        }
    }

    // Begin dragging the tile
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalPosition = transform.localPosition;
        originalIndex = deckManager.Hand.IndexOf(gameObject);
        currentPreviewIndex = originalIndex;
        
        // Lift the tile slightly forward
        transform.localPosition += new Vector3(0, 0, -originalZOffset);
    }

    // While dragging the tile
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Move tile with cursor in camera space (on the X axis only)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, mainCamera.WorldToScreenPoint(transform.position).z);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            
            // Lock Y and Z, only update X position
            transform.position = new Vector3(worldPos.x, transform.position.y, transform.position.z);
            
            // Calculate where this tile would be inserted
            int newPreviewIndex = CalculateInsertionIndex();
            
            // If preview position changed, reposition other tiles
            if (newPreviewIndex != currentPreviewIndex)
            {
                currentPreviewIndex = newPreviewIndex;
                RepositionHandWithGap(currentPreviewIndex);
            }

            // Hide tooltip while dragging
            if (tooltip != null && tooltip.transform.childCount > 0)
            {
                tooltip.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    // End dragging the tile
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        // Use the preview index as the final position
        if (currentPreviewIndex != originalIndex && currentPreviewIndex >= 0)
        {
            // Reorder the hand list
            deckManager.Hand.RemoveAt(originalIndex);
            deckManager.Hand.Insert(currentPreviewIndex, gameObject);
        }
        
        // Reposition all tiles normally
        deckManager.sortHand();
        
        // Restore tooltip if still hovering
        if (isHoveringOverTile && tooltip != null && tooltip.transform.childCount > 0)
        {
            tooltip.transform.GetChild(0).gameObject.SetActive(true);
            UpdateTooltipPosition();
        }
    }

    // Calculate the index where the dragged tile should be inserted
    private int CalculateInsertionIndex()
    {
        List<GameObject> hand = deckManager.Hand;
        if (hand.Count <= 1) return 0;

        // Get the x position of this tile in local space relative to parent
        float thisX = transform.localPosition.x;
        
        // Calculate the expected x position for each index
        int handCount = hand.Count;
        float tileSpacing = 0.25f;
        float startX = -((handCount - 1) * tileSpacing / 2f);
        
        // Find the closest slot
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < handCount; i++)
        {
            float slotX = startX + (i * tileSpacing);
            float distance = Mathf.Abs(thisX - slotX);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }

    // Reposition the hand tiles, leaving a gap at the specified index for the dragged tile
    // This creates a dynamic gap to make it easier to tell where the tile will be placed in the hand
    private void RepositionHandWithGap(int gapIndex)
    {
        List<GameObject> hand = deckManager.Hand;
        int handCount = hand.Count;
        float tileSpacing = 0.25f;
        float startX = -((handCount - 1) * tileSpacing / 2f);
        
        // Build a list of tiles in their current order (excluding the dragged tile)
        List<GameObject> otherTiles = new List<GameObject>();
        for (int i = 0; i < handCount; i++)
        {
            if (i != originalIndex)
            {
                otherTiles.Add(hand[i]);
            }
        }
        
        // Position each tile, leaving a gap at gapIndex for the dragged tile
        for (int i = 0; i < otherTiles.Count; i++)
        {
            GameObject tile = otherTiles[i];
            
            // Determine visual position (make room for the gap)
            int visualPos = i;
            if (i >= gapIndex)
            {
                visualPos = i + 1; // Shift right to leave gap
            }
            
            float targetX = startX + (visualPos * tileSpacing);
            
            // Preserve Y position (important for selected tiles that are raised)
            float currentY = tile.transform.localPosition.y;
            float currentZ = tile.transform.localPosition.z;
            Vector3 targetPos = new Vector3(targetX, currentY, currentZ);
            
            // Smoothly move tiles to their new positions
            tile.transform.localPosition = Vector3.Lerp(tile.transform.localPosition, targetPos, 1f); // <- This is the value that determines how much tiles move visually
        }
    }

    // Select the tile
    void clicked()
    {
        // Only process clicks if not dragging and in selection mode
        if (GameManager.Instance.selecting && tileData != null)
        {
            if (deckManager.selectedTiles.Contains(gameObject))
                removeFromSelection();
            else
            {
                if (deckManager.selectedTiles.Count < deckManager.MAX_DISCARD_SELECTION)
                    addToSelection();
            }
        }
    }
    void addToSelection()
    {
        deckManager.selectedTiles.Add(gameObject);

        //temp move forward to show selection, will replace with actual animation later
        gameObject.transform.localPosition += new Vector3(0, 0.125f, 0);
        
        // Update tooltip position if hovering
        if (isHoveringOverTile)
        {
            UpdateTooltipPosition();
        }
    }
    void removeFromSelection()
    {
        deckManager.selectedTiles.Remove(gameObject);
        //temp move back to show deselection, will replace with actual animation later
        gameObject.transform.localPosition -= new Vector3(0, 0.125f, 0);
        
        // Update tooltip position if hovering
        if (isHoveringOverTile)
        {
            UpdateTooltipPosition();
        }
    }
}
