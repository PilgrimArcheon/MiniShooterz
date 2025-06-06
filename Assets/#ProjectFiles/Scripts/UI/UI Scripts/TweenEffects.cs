using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class TweenEffects : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform targetRectTransform;
    [SerializeField] private EffectType effectType;
    [Header("Pulsate")]
    [SerializeField] private float pulsateDuration = 0.5f;
    [SerializeField] private float pulsateScale = 1.2f;
    [Header("Jiggle")]
    [SerializeField] private float jiggleDuration = 0.5f;
    [SerializeField] private float jiggleStrength = 10f;
    [SerializeField] private int jiggleVibrato = 10;
    [Space]
    [Header("Loop")] 
    [SerializeField] private bool loop = true;
    [SerializeField] private int numberOfLoops;
    [SerializeField] private float loopDuration;

    Vector3 defRectSize;
    Vector3 defRectScale;
    Vector3 defRectPos;
    bool canDoLoop;

    private void OnEnable()
    {
        targetRectTransform = GetComponent<RectTransform>();
        defRectSize = targetRectTransform.sizeDelta;
        defRectScale = targetRectTransform.localScale;
        defRectPos = targetRectTransform.anchoredPosition;
        canDoLoop = loop;
        DoEffect();
    }

    public void DoEffect()
    {
        switch (effectType)
        {
            case EffectType.Jiggle:
                Jiggle();
                break;
            case EffectType.Pulsate:
                Pulsate();
                break;
            default:
                break;
        }
    }

    /// Creates a pulsating effect by scaling up and down the RectTransform.
    public void Pulsate()
    {
        // Animate scale to create a pulsating effect
        targetRectTransform.DOScale(Vector3.one * pulsateScale, pulsateDuration / 2)
            .SetEase(Ease.InOutSine)
            .SetLoops(numberOfLoops, LoopType.Yoyo) // Scale up and then back down
            .OnComplete(() =>
            {
                targetRectTransform.localScale = defRectScale;
                if (canDoLoop) Invoke(nameof(Pulsate), loopDuration);
            });
    }

    /// Creates a jiggle effect by shaking the RectTransform.
    public void Jiggle()
    {
        // Animate shake to create a jiggle effect
        targetRectTransform.DOShakeAnchorPos(jiggleDuration, jiggleStrength, jiggleVibrato, 90f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                if (canDoLoop) Invoke(nameof(Jiggle), loopDuration);
            });
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        canDoLoop = loop; // Make Sure Lopping is Enabled or stopped When Needed
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        canDoLoop = false; // Set Looping to false when Finger is on Screen
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canDoLoop = false; // Set Looping to false when Finger is dragging across the screen
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canDoLoop = loop; // Make Sure Lopping is Enabled or stopped When Needed
    }
}


[System.Serializable]
public enum EffectType
{
    Pulsate,
    Jiggle
}