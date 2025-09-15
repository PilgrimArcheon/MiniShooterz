using UnityEngine;
using UnityEngine.EventSystems;

public class FixedButton : MonoBehaviour, IPointerDownHandler
{
    public bool IsActive;
    [SerializeField] protected CanvasGroup background = null;
    public bool IsMobile { get { return GameManager.Instance.forcedMobile || MenuManager.Instance.IsWebMobile(); } }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsMobile) return;

        IsActive = !IsActive;
        background.alpha = IsActive ? 0 : 1;
    }
}