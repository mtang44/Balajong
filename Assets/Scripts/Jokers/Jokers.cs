using UnityEngine;

public class Jokers 
{
    public string rarity;
    public string name;
    // public int quantity;
    public string equationType;

    public string equation;
    
    public string description;
    public int price;
    public Jokers(string name, string rarity, string equationType,  string description, int price)
    {
        this.name = name;
        this.rarity = rarity;
        this.equationType = equationType;
        this.description = description;
        this.price = price;
    }
}