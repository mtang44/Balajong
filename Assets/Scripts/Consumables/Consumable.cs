
public class Consumable 
{
    public string rarity;
    public string name;
    // public int quantity;
    public string equationType;
    
    public string description;
    public string code;
    public int price;
    public Consumable(string name, string rarity, string code, string equationType, string description, int price)
    {
        this.name = name;
        this.rarity = rarity;
        this.code = code;
        this.equationType = equationType;
        this.description = description;
        this.price = price;
    }

    // Copy constructor for carrying a bought consumable into inventory (shop may unload).
    public Consumable(Consumable other)
    {
        if (other == null) return;
        name = other.name;
        rarity = other.rarity;
        code = other.code;
        equationType = other.equationType;
        description = other.description;
        price = other.price;
    }
}