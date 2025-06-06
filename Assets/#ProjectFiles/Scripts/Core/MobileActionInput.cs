using UnityEngine;
using UnityEngine.EventSystems;


public class MobileActionInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsActive;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsActive = false;
    }
}
