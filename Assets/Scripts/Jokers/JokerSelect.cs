using UnityEngine;

public class JokerSelect : MonoBehaviour
{
    public string code;
    public string name;
    public string description;

    public void Initialize(string code, string name, string description)
    {
        this.code = code;
        this.name = name;
        this.description = description;
    }
}
