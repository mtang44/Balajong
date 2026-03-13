using UnityEngine;
using TMPro;

public class UpdateDiscardOnEnable : MonoBehaviour
{
    public void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            int discardsLeft = GameManager.Instance.maxDiscards - GameManager.Instance.currentDiscards;
            this.GetComponent<TextMeshProUGUI>().text = discardsLeft.ToString();
        }
    }
}
