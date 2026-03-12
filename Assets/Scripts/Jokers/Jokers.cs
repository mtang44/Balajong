
public class Jokers 
{
    public string rarity;
    public string name;
    // public int quantity;
    public string equationType;

    public string equation;
    
    public string description;
    public string code;
    public int price;
    public Jokers(string name, string rarity, string code,string equationType, string description, int price)
    {
        this.name = name;
        this.rarity = rarity;
        this.code = code;
        this.equationType = equationType;
        
        this.description = description;
        this.price = price;
    }
}