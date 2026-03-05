using UnityEngine;

public class BlindPanelMover : MonoBehaviour
{
    [SerializeField] private Vector2 raisedPosition;
    [SerializeField] private Vector2 loweredPosition;

    private RectTransform rect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
     public void RaisePanel()
    {
        rect.anchoredPosition = raisedPosition;
    }
    public void LowerPanel()
    {
        rect.anchoredPosition = loweredPosition;
    }
}
