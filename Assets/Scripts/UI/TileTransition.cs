using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(MahjongTileHolder))]
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
    [SerializeField] private bool randomizeEdition = true;
    [SerializeField] public int[] typeWeight;
    [SerializeField] public int[] editionWeight;

    private MahjongTileHolder tileHolder;
    private bool restoredSavedTile;

    private static readonly int TileTypeCount = System.Enum.GetValues(typeof(TileType)).Length;
    private static readonly int EditionCount = System.Enum.GetValues(typeof(Edition)).Length;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasSavedTile = false;
    }

    private void Awake()
    {
        tileHolder = GetComponent<MahjongTileHolder>();
        EnsureWeightArrays();

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
        EnsureWeightArrays();
    }

    private System.Collections.IEnumerator Start()
    {
        if (!preserveAcrossScenes || restoredSavedTile)
        {
            yield break;
        }

        // Wait one frame so RandomTileGen.Start can finish its randomization first.
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

        TileType randomType = (TileType)GetWeightedIndex(typeWeight, TileTypeCount);

        NumberedValue numberedValue = NumberedValue.One;
        WindValue windValue = WindValue.North;
        DragonValue dragonValue = DragonValue.Red;
        FlowerValue flowerValue = FlowerValue.Plum;
        SeasonValue seasonValue = SeasonValue.Spring;

        switch (randomType)
        {
            case TileType.Dots:
            case TileType.Bam:
            case TileType.Crack:
                numberedValue = (NumberedValue)Random.Range(1, 10);
                break;
            case TileType.Wind:
                windValue = (WindValue)Random.Range(0, 4);
                break;
            case TileType.Dragon:
                dragonValue = (DragonValue)Random.Range(0, 3);
                break;
            case TileType.Flower:
                flowerValue = (FlowerValue)Random.Range(0, 4);
                break;
            case TileType.Season:
                seasonValue = (SeasonValue)Random.Range(0, 4);
                break;
        }

        Edition edition = Edition.Base;
        if (randomizeEdition)
        {
            edition = (Edition)GetWeightedIndex(editionWeight, EditionCount);
        }
        else if (tileHolder.TileData != null)
        {
            edition = tileHolder.TileData.Edition;
        }

        MahjongTileData tileData = new MahjongTileData(
            randomType,
            numberedValue,
            windValue,
            dragonValue,
            flowerValue,
            seasonValue,
            edition);

        tileHolder.SetTileData(tileData);
        SaveCurrentTile();
    }

    private void EnsureWeightArrays()
    {
        EnsureWeightArrayLength(ref typeWeight, TileTypeCount, 1);
        EnsureWeightArrayLength(ref editionWeight, EditionCount, 1);
    }

    private static void EnsureWeightArrayLength(ref int[] weights, int targetLength, int defaultValue)
    {
        if (weights == null)
        {
            weights = new int[targetLength];
            for (int i = 0; i < targetLength; i++)
            {
                weights[i] = defaultValue;
            }
            return;
        }

        if (weights.Length == targetLength)
        {
            return;
        }

        int[] resized = new int[targetLength];
        int copyCount = Mathf.Min(weights.Length, targetLength);
        for (int i = 0; i < copyCount; i++)
        {
            resized[i] = weights[i];
        }

        for (int i = copyCount; i < targetLength; i++)
        {
            resized[i] = defaultValue;
        }

        weights = resized;
    }

    private static int GetWeightedIndex(int[] weights, int optionCount)
    {
        if (optionCount <= 0)
        {
            return 0;
        }

        int totalWeight = 0;
        for (int i = 0; i < optionCount; i++)
        {
            if (weights == null || i >= weights.Length)
            {
                continue;
            }

            totalWeight += Mathf.Max(0, weights[i]);
        }

        if (totalWeight <= 0)
        {
            return Random.Range(0, optionCount);
        }

        int roll = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < optionCount; i++)
        {
            int weight = 0;
            if (weights != null && i < weights.Length)
            {
                weight = Mathf.Max(0, weights[i]);
            }

            cumulativeWeight += weight;
            if (roll < cumulativeWeight)
            {
                return i;
            }
        }

        return optionCount - 1;
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
