using UnityEngine;
using UnityEngine.EventSystems;

public class HandExampleHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private HandExampleManager manager;
    private int index = -1;

    public void Configure(HandExampleManager owner, int hoverIndex)
    {
        manager = owner;
        index = hoverIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        manager?.HandlePointerEnter(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        manager?.HandlePointerExit(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager?.HandlePointerClick(index);
    }
}
