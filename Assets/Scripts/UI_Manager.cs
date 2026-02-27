using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UI_Manager : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject runInfoPanel;
    [SerializeField] private GameObject playerStatsPanel;
    [SerializeField] private GameObject runInfoBTN;
    [SerializeField] private GameObject nextRoundBTN;
    [SerializeField] private GameObject rerollShopBTN;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runInfoBTN.GetComponent<Button>().onClick.AddListener(DisplayRunInfo);
        nextRoundBTN.GetComponent<Button>().onClick.AddListener(NextRound);
        rerollShopBTN.GetComponent<Button>().onClick.AddListener(RerollShop);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void DisplayRunInfo()
    {
        // code to dislay run info ui
    }
    public void NextRound()
    {
        // code to start selection of next Bind enemy
    }
    public void RerollShop()
    {
        // code to reroll shop items 
        // should call "loot chest" script 
        //rerollShopBTN.GetComponentInChildren<TextMeshProUGUI>().text = "Reroll $" + // code to get new cost after reroll ;
    }
}
