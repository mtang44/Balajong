using UnityEngine;

// This script is attached to the MahjongTile GameObject. It handles collision interaction, and will add itself to the DeckManager on click
public class TileSelect : MonoBehaviour
{
    DeckManager deckManager;
    private MahjongTileData tileData;

    void Start()
    {
        deckManager = DeckManager.Instance;
        tileData = GetComponent<MahjongTileHolder>().TileData;
    }
    void clicked()
    {
        if (deckManager.selectedTiles.Count < deckManager.MAX_DISCARD_SELECTION)
        {
            if(!deckManager.selectedTiles.Contains(gameObject))
                addToSelection();
            else
                removeFromSelection();
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
