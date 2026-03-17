using UnityEngine;
using System.Collections.Generic;

public class EnemyInformationGrammer : MonoBehaviour
{
    public static List<string> firstNames = new List<string>
    {
        "Muddy", "Giddy", "Anxious", "Awesome", "Small",
        "Big", "Wide", "Saucy", "Damp", "Puzzled",
        "Long", "Lowly", "Funny", "Furry", "Awful",
        "Sorry", "Icy", "Old", "Young", "Little",
        "Quick", "Skinny", "Strange", "Strong", "Tired"
    };
    public static List<string> lastNames = new List<string>
    {
        "Health", "Mode", "Beer", "Song", "Growth",
        "Tongue", "Tea", "Church", "Night", "Wealth",
        "Art", "Fact", "Dad", "Mom", "Death",
        "Pie", "Law", "Hat", "Card", "Tile",
        "Debt", "Gene", "Soup", "Mouse", "Spouse"
    };
    public static List<string> titles = new List<string>
    {
        "Sir", "Lady", "Dr.", "Mr.", "Mrs.", "Lord", "King", "Queen", "Emp.", "Pres."
    };
    public static List<string> bossNames = new List<string>
    {
        "The Bone", "The Soap", "The Lloyd", "The Smaug", "The Wash",
        "The East", "The West", "The North", "The South", "Tyler Coleman"
    };
    public static List<int> scoreThresholds = new List<int>
    {
        150, 300, 600, 1200, 2400, 4800, 9600, 19200, 38400, 76800, 153600, 307200, 614400, 1228800, 2457600, 4915200, 9830400, 19660800, 39321600, 78643200, 157286400, 314572800, 629145600, 999999999
    };
}
