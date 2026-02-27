using UnityEngine;

public enum TileType
{
    Dots,
    Bam,
    Crack,
    Wind,
    Dragon,
    Flower,
    Season
}

public enum NumberedValue
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9
}

public enum WindValue
{
    North,
    East,
    South,
    West
}

public enum DragonValue
{
    Red,
    Green,
    White
}

public enum FlowerValue
{
    Plum,
    Orchid,
    Bamboo,
    Chrysanthemum
}

public enum SeasonValue
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public class MahjongTileData : MonoBehaviour
{
    [SerializeField]
    private TileType tileType;

    [SerializeField]
    private NumberedValue numberedValue;

    [SerializeField]
    private WindValue windValue;

    [SerializeField]
    private DragonValue dragonValue;

    [SerializeField]
    private FlowerValue flowerValue;

    [SerializeField]
    private SeasonValue seasonValue;

    [SerializeField]
    private Sprite sprite;

    public TileType TileType => tileType;
    public NumberedValue NumberedValue => numberedValue;
    public WindValue WindValue => windValue;
    public DragonValue DragonValue => dragonValue;
    public FlowerValue FlowerValue => flowerValue;
    public SeasonValue SeasonValue => seasonValue;
    public Sprite Sprite => sprite;

    public void SetSprite(Sprite newSprite)
    {
        sprite = newSprite;
    }

    public string GetTileDisplayName()
    {
        return tileType switch
        {
            TileType.Dots => $"{numberedValue} Dots",
            TileType.Bam => $"{numberedValue} Bam",
            TileType.Crack => $"{numberedValue} Crack",
            TileType.Wind => $"{windValue} Wind",
            TileType.Dragon => $"{dragonValue} Dragon",
            TileType.Flower => $"{flowerValue} Flower",
            TileType.Season => $"{seasonValue} Season",
            _ => "Unknown Tile"
        };
    }
}
