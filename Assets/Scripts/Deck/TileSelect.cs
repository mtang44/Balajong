using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// This script is attached to the MahjongTile GameObject. It handles collision interaction, and will add itself to the DeckManager on click
public class TileSelect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    DeckManager deckManager;
    public MahjongTileData tileData;

    private Vector3 originalPosition;
    private int originalIndex;
    private int currentPreviewIndex;
    private bool isDragging = false;
    private float dragStartTime;
    private bool leftPointerDown;
    private float originalZOffset = 0.5f; // How far forward to lift the tile
    bool flowerTile = false;
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
        if(tileData != null && (tileData.TileType == TileType.Flower || tileData.TileType == TileType.Season))
        {
            flowerTile = true;
        }
    }

    // Pick the tile up - begin drag
    public void OnPointerDown(PointerEventData eventData)
    {
        if(flowerTile) return; 

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            leftPointerDown = false;
            return;
        }

        leftPointerDown = true;
        dragStartTime = Time.time;
    }

    // Drop the tile - end drag
    public void OnPointerUp(PointerEventData eventData)
    {
        if(flowerTile) return; 

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            leftPointerDown = false;
            return;
        }

        if (!leftPointerDown)
        {
            return;
        }

        leftPointerDown = false;

        // If we didn't drag (quick click), process as selection
        if (!isDragging && Time.time - dragStartTime < 0.2f)
        {
            clicked();
        }
    }

    // Begin dragging the tile
    public void OnBeginDrag(PointerEventData eventData)
    {
        if(flowerTile) return; 
        if (eventData.button != PointerEventData.InputButton.Left) return;
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
        if(flowerTile) return; 
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
        }
    }

    // End dragging the tile
    public void OnEndDrag(PointerEventData eventData)
    {
        if(flowerTile) return; 
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!isDragging) return;
        isDragging = false;
        leftPointerDown = false;

        // Use the preview index as the final position
        if (currentPreviewIndex != originalIndex && currentPreviewIndex >= 0)
        {
            // Reorder the hand list
            deckManager.Hand.RemoveAt(originalIndex);
            deckManager.Hand.Insert(currentPreviewIndex, gameObject);
        }
        
        // Reposition all tiles normally
        deckManager.sortHand();
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
        if(flowerTile) return; 
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

    // Select the tile (normal discard/check-rack selection, or consumable flow e.g. Add: pick 1 tile, then pick 4 to discard)
    void clicked()
    {
        if (flowerTile) return;
        if (tileData == null) return;
        bool inSelectionMode = (GameManager.Instance != null && GameManager.Instance.selecting)
            || ConsumableEffectSystem.InTileSelectionPhase;
        if (!inSelectionMode) return;

        if (deckManager.selectedTiles.Contains(gameObject))
            removeFromSelection();
        else if (deckManager.selectedTiles.Count < deckManager.MAX_DISCARD_SELECTION)
            addToSelection();
    }
    void addToSelection()
    {
        if(flowerTile) return; 
        deckManager.selectedTiles.Add(gameObject);

        //temp move forward to show selection, will replace with actual animation later
        gameObject.transform.localPosition += new Vector3(0, 0.125f, 0);
    }
    void removeFromSelection()
    {
        if(flowerTile) return; 
        deckManager.selectedTiles.Remove(gameObject);
        //temp move back to show deselection, will replace with actual animation later
        gameObject.transform.localPosition -= new Vector3(0, 0.125f, 0);
    }
}
