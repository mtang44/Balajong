using UnityEngine;

public class MahjongTileHolder : MonoBehaviour
{
    [SerializeField]
    private MahjongTileData tileData;
    public MahjongTileData TileData => tileData;
    
    private void OnEnable()
    {
        // Initialize tileData if it's null
        if (tileData == null)
        {
            tileData = new MahjongTileData(TileType.Dots, NumberedValue.One);
        }
    }
    
    public void SetTileData(MahjongTileData newData)
    {
        tileData = newData;
    }

    public void OnValidate()
    {
        // Notify display component to update when data changes
        MahjongTileDisplay display = GetComponent<MahjongTileDisplay>();
        if (display != null)
        {
            display.ApplyTileSprite();
        }
    }
}
