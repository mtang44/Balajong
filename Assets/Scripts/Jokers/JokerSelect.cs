using UnityEngine;

public class JokerSelect : MonoBehaviour
{
    public string code;
    public string name;
    public string description;
    public int price;

    public void Initialize(string code, string name, string description, int price)
    {
        this.code = code;
        this.name = name;
        this.description = description;
        this.price = price;
    }
    public void SellJoker()
    {
        JokerHolderUI.Instance.RemoveJoker(JokerManager.Instance.jokers.IndexOf(code));
        JokerManager.Instance.jokers.Remove(code);
        PlayerStatManager.Instance.cash += price / 2;
    }
}
