using UnityEngine;
using UnityEngine.EventSystems;

// This script is attached to the MahjongTile GameObject. It handles collision interaction, and will add itself to the DeckManager on click
public class TileSelect : MonoBehaviour, IPointerDownHandler
{
    DeckManager deckManager;
    public MahjongTileData tileData;

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
    }
    void clicked()
    {
        if (deckManager.selectedTiles.Contains(gameObject))
            removeFromSelection();
        else {
            if(deckManager.selectedTiles.Count < deckManager.MAX_DISCARD_SELECTION)
                addToSelection();
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.selecting && tileData != null)
        {
            clicked();
            //Debug.Log("Clicked tile: " + tileData.GetTileDisplayName());
        }
    }
    void addToSelection()
    {
        deckManager.selectedTiles.Add(gameObject);
    }
    void removeFromSelection()
    {
        deckManager.selectedTiles.Remove(gameObject);
    }
}
