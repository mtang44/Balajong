using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MahjongTileHolder))]
[RequireComponent(typeof(TileGenerator))]
public class TileTransition : MonoBehaviour
{
    [System.Serializable]
    private struct SavedTileData
    {
        public TileType tileType;
        public NumberedValue numberedValue;
        public WindValue windValue;
        public DragonValue dragonValue;
        public FlowerValue flowerValue;
        public SeasonValue seasonValue;
        public Edition edition;
    }

    private static bool hasSavedTile;
    private static SavedTileData savedTile;

    [SerializeField] private bool preserveAcrossScenes = true;
    [SerializeField] private TileGenerator tileGenerator;

    private MahjongTileHolder tileHolder;
    private bool restoredSavedTile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasSavedTile = false;
    }

    private void Awake()
    {
        tileHolder = GetComponent<MahjongTileHolder>();
        tileGenerator = ResolveTileGenerator();

        if (!preserveAcrossScenes)
        {
            return;
        }

        if (hasSavedTile)
        {
            RestoreSavedTile();
            restoredSavedTile = true;
        }
    }

    private void OnValidate()
    {
        if (tileGenerator == null)
        {
            tileGenerator = GetComponent<TileGenerator>();
        }
    }

    private System.Collections.IEnumerator Start()
    {
        if (!preserveAcrossScenes || restoredSavedTile)
        {
            yield break;
        }

        // Wait one frame so any Start-time randomization can finish first.
        yield return null;
        SaveCurrentTile();
    }

    private void OnDestroy()
    {
        if (!preserveAcrossScenes)
        {
            return;
        }

        SaveCurrentTile();
    }

    [ContextMenu("Clear Saved Transition Tile")]
    public void ClearSavedTransitionTile()
    {
        hasSavedTile = false;
    }

    public void RandomizeAndSave()
    {
        if (tileHolder == null)
        {
            tileHolder = GetComponent<MahjongTileHolder>();
        }

        if (tileHolder == null)
        {
            Debug.LogWarning("TileTransition requires a MahjongTileHolder on the same GameObject.", this);
            return;
        }

        tileGenerator = ResolveTileGenerator();
        if (tileGenerator == null)
        {
            Debug.LogWarning("TileTransition requires a TileGenerator on the same GameObject.", this);
            return;
        }

        if (!tileGenerator.TryRandomizeCurrentTile())
        {
            return;
        }

        SaveCurrentTile();
    }

    private TileGenerator ResolveTileGenerator()
    {
        if (tileGenerator != null)
        {
            return tileGenerator;
        }

        tileGenerator = GetComponent<TileGenerator>();
        if (tileGenerator == null)
        {
            tileGenerator = gameObject.AddComponent<TileGenerator>();
        }

        return tileGenerator;
    }

    private void SaveCurrentTile()
    {
        if (tileHolder == null || tileHolder.TileData == null)
        {
            return;
        }

        MahjongTileData data = tileHolder.TileData;
        savedTile = new SavedTileData
        {
            tileType = data.TileType,
            numberedValue = data.NumberedValue,
            windValue = data.WindValue,
            dragonValue = data.DragonValue,
            flowerValue = data.FlowerValue,
            seasonValue = data.SeasonValue,
            edition = data.Edition
        };

        hasSavedTile = true;
    }

    private void RestoreSavedTile()
    {
        MahjongTileData restoredData = new MahjongTileData(
            savedTile.tileType,
            savedTile.numberedValue,
            savedTile.windValue,
            savedTile.dragonValue,
            savedTile.flowerValue,
            savedTile.seasonValue,
            savedTile.edition);

        tileHolder.SetTileData(restoredData);
    }
}
