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
    None = 0,
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
    None,
    North,
    East,
    South,
    West
}

public enum DragonValue
{
    None,
    Red,
    Green,
    White
}

public enum FlowerValue
{
    None,
    Plum,
    Orchid,
    Bamboo,
    Chrysanthemum
}

public enum SeasonValue
{
    None,
    Spring,
    Summer,
    Autumn,
    Winter
}

//[CreateAssetMenu(fileName = "New Mahjong Tile", menuName = "Mahjong/Tile")]
public class MahjongTile
{
    public MahjongTile(string value, string suit)
    {
        switch (suit)
        {
            case "C":
                tileType = TileType.Crack;
                break;
            case "B":
                tileType = TileType.Bam;
                break;
            case "O":
                tileType = TileType.Dots;
                break;
            case "W":
                tileType = TileType.Wind;
                break;
            case "D":
                tileType = TileType.Dragon;
                break;
            case "F":
                tileType = TileType.Flower;
                break;
            case "S":
                tileType = TileType.Season;
                break;
        }
        if (tileType == TileType.Crack || tileType == TileType.Bam || tileType == TileType.Dots)
        {
            numberedValue = (NumberedValue)int.Parse(value);
        }
        else if (tileType == TileType.Wind)
        {
            windValue = value switch
            {
                "1" => WindValue.North,
                "2" => WindValue.East,
                "3" => WindValue.South,
                "4" => WindValue.West,
                _ => throw new System.ArgumentException("Invalid wind value")
            };
        }
        else if (tileType == TileType.Dragon)
        {
            dragonValue = value switch
            {
                "1" => DragonValue.Red,
                "2" => DragonValue.Green,
                "3" => DragonValue.White,
                _ => throw new System.ArgumentException("Invalid dragon value")
            };
        }
        else if (tileType == TileType.Flower)
        {
            flowerValue = value switch
            {
                "1" => FlowerValue.Plum,
                "2" => FlowerValue.Orchid,
                "3" => FlowerValue.Chrysanthemum,
                "4" => FlowerValue.Bamboo,
                _ => throw new System.ArgumentException("Invalid flower value")
            };
        }
        else if (tileType == TileType.Season)
        {
            seasonValue = value switch
            {
                "1" => SeasonValue.Spring,
                "2" => SeasonValue.Summer,
                "3" => SeasonValue.Autumn,
                "4" => SeasonValue.Winter,
                _ => throw new System.ArgumentException("Invalid season value")
            };
        }
    }
    public MahjongTile(MahjongTile otherTile)
    {
        tileType = otherTile.TileType;
        numberedValue = otherTile.NumberedValue;
        windValue = otherTile.WindValue;
        dragonValue = otherTile.DragonValue;
        flowerValue = otherTile.FlowerValue;
        seasonValue = otherTile.SeasonValue;
        sprite = otherTile.Sprite;
    }

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
