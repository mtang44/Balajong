using UnityEngine;
using TMPro;

public class UpdateCashOnEnable : MonoBehaviour
{
    public void Start()
    {
        if (PlayerStatManager.Instance != null)
        {
            int cash = PlayerStatManager.Instance.cash;
            this.GetComponent<TextMeshProUGUI>().text = "$" + cash.ToString();
        }
    }
}
