using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MahjongTileHolder))]
public class TileGenerator : MonoBehaviour
{
	[SerializeField] private bool randomizeOnStart = false;
	[SerializeField] private bool randomizeEdition = true;
	[SerializeField] public int[] typeWeight;
	[SerializeField] public int[] editionWeight;

	private MahjongTileHolder tileHolder;

	private static readonly int TileTypeCount = System.Enum.GetValues(typeof(TileType)).Length;
	private static readonly int EditionCount = System.Enum.GetValues(typeof(Edition)).Length;

	private void Awake()
	{
		tileHolder = GetComponent<MahjongTileHolder>();
		EnsureWeightArrays();
	}

	private void OnValidate()
	{
		EnsureWeightArrays();
	}

	private void Start()
	{
		if (randomizeOnStart)
		{
			TryRandomizeCurrentTile();
		}
	}

	[ContextMenu("Randomize Current Tile")]
	public void RandomizeCurrentTile()
	{
		TryRandomizeCurrentTile();
	}

	public bool TryRandomizeCurrentTile()
	{
		tileHolder ??= GetComponent<MahjongTileHolder>();
		if (tileHolder == null)
		{
			Debug.LogWarning("TileGenerator requires a MahjongTileHolder on the same GameObject.", this);
			return false;
		}

		MahjongTileData tileData = BuildRandomTileData(tileHolder.TileData);
		tileHolder.SetTileData(tileData);
		return true;
	}

	public MahjongTileData BuildRandomTileData(MahjongTileData currentTileData = null)
	{
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
		else if (currentTileData != null)
		{
			edition = currentTileData.Edition;
		}

		return new MahjongTileData(
			randomType,
			numberedValue,
			windValue,
			dragonValue,
			flowerValue,
			seasonValue,
			edition);
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
}
