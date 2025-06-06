using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    public bool IsActive;
    public bool IsMobile { get { return GameManager.Instance.forcedMobile || MenuManager.Instance.IsWebMobile(); } }
    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!IsMobile) return;
        
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        IsActive = true;
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        background.gameObject.SetActive(false);
        IsActive = false;
        base.OnPointerUp(eventData);
    }
}