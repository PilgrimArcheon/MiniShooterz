using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class UIContentTween : MonoBehaviour
{
    // Reference to the RectTransform component
    RectTransform rectTransform;

    // Initial position of the UI content
    [SerializeField] Vector2 startPos;

    // Initial size of the UI content
    [SerializeField] Vector2 startSize;

    // Time it takes to arrange the UI content
    [SerializeField] float arrangementTime = 0.25f;

    // Flag to check if the keyboard is being used
    [SerializeField] bool onUseKeyboard;

    // Flag to check if the UI content is modal
    [SerializeField] bool isModal;

    // Initial scale of the UI content
    Vector2 startScale;

    // Flag to check if the UI content should stay up
    bool stayUp;

    // Flag to check if the modal ID is down
    bool checkModalIDown;

    [SerializeField] UnityEvent OnEnabledEvent;

    // Called when the script is initialized
    void Awake() => SetUpMenuContent();

    void OEnable() => OnEnabledEvent.Invoke();

    // Sets up the initial state of the UI content
    void SetUpMenuContent()
    {
        // Get the RectTransform component
        rectTransform = GetComponent<RectTransform>();

        // Store the initial position and size of the UI content
        startPos = rectTransform.anchoredPosition;
        startSize = rectTransform.sizeDelta;
        startScale = rectTransform.localScale;
    }

    // Called every frame
    void Update()
    {
        // Check if the keyboard is being used
        if (onUseKeyboard)
        {
            // Check if the keyboard is visible
            if (TouchScreenKeyboard.visible)
                // Adjust the UI content for the keyboard
                AdjustUIForKeyboard();
            else
            {
                // Check if the modal ID is not down
                if (!checkModalIDown)
                    // Reset the UI content after a delay
                    DoDelayReset();
            }
        }
    }

    public void DoDelayReset()
    {
        // Start the coroutine to reset the UI content after a delay
        StartCoroutine(DoResetTween());
    }

    IEnumerator DoResetTween()
    {
        // Wait for a short delay before resetting the UI content
        yield return new WaitForSeconds(0.05f);

        // Reset the size of the UI content to its initial size
        rectTransform.DOSizeDelta(startSize, arrangementTime);

        // Reset the position of the UI content to its initial position
        rectTransform.DOAnchorPos(startPos, arrangementTime);

        // If the UI content is modal, set the flag to indicate that the modal ID is down
        if (isModal) checkModalIDown = true;
    }

    public void AdjustUIForKeyboard()
    {
        // If the UI content is modal, reset the flag to indicate that the modal ID is not down
        if (isModal) checkModalIDown = false;

#if !UNITY_WEBGL
        // Get the height of the keyboard
        float rectPosition = GetKeyboardHeight();

        // If the position of the UI content is not equal to the keyboard height, move the UI content to the keyboard height
        if (rectTransform.position.y != rectPosition) rectTransform.DOMoveY(rectPosition, arrangementTime);

        // If the UI content is not modal, adjust its size to fit the keyboard
        if (!isModal)
        {
            // If the width of the UI content is not equal to 3500f, set its width to 3500f
            if (rectTransform.sizeDelta.x != 3500f) rectTransform.DOSizeDelta(new Vector2(3500f, rectTransform.sizeDelta.y), arrangementTime);
        }
#endif
    }

//     public int GetKeyboardHeight()
//     {
//         // Get the height of the keyboard based on the platform
// #if UNITY_EDITOR
//         // In the Unity Editor, the keyboard height is 0
//         return 0;
// #elif UNITY_ANDROID
//     // On Android, get the keyboard height using the AndroidJavaClass
//     using (AndroidJavaClass UnityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
//     {
//         AndroidJavaObject View = UnityClass.GetStatic<AndroidJavaObject>("currentActivity").Get<AndroidJavaObject>("mUnityPlayer").Call<AndroidJavaObject>("getView");

//         using (AndroidJavaObject Rct = new AndroidJavaObject("android.graphics.Rect"))
//         {
//             View.Call("getWindowVisibleDisplayFrame", Rct);

//             // Return the height of the screen minus the height of the visible display frame
//             return Screen.height - Rct.Call<int>("height");
//         }
//     }
// #elif UNITY_IOS
//     // On iOS, get the keyboard height using the TouchScreenKeyboard
//     var area = TouchScreenKeyboard.area;
//     var height = Mathf.RoundToInt(area.height);
//     // Return the height of the keyboard, or 0 if the screen height is less than or equal to the keyboard height
//     return Screen.height <= height ? 0 : height;
// #else 
//     return 0;
// #endif
//     }

    // Tween the UI content to a specific position on the X-axis
    public void TweenToPositionX(float tweenPosX)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current X-position is different from the target position
        if (rectTransform.anchoredPosition.x != tweenPosX)
            // Tween the UI content to the target X-position
            rectTransform.DOAnchorPos(new Vector2(tweenPosX, rectTransform.anchoredPosition.y), arrangementTime);
    }

    // Tween the UI content to a specific position on the Y-axis
    public void TweenToPositionY(float tweenPosY)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current Y-position is different from the target position
        if (rectTransform.anchoredPosition.y != tweenPosY)
            // Tween the UI content to the target Y-position
            rectTransform.DOAnchorPos(new Vector2(rectTransform.anchoredPosition.x, tweenPosY), arrangementTime);
    }

    // Tween the UI content to a specific size on the X-axis
    public void TweenToSizeX(float tweenSizeX)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current X-size is different from the target size
        if (rectTransform.sizeDelta.x != tweenSizeX)
            // Tween the UI content to the target X-size
            rectTransform.DOSizeDelta(new Vector2(tweenSizeX, rectTransform.sizeDelta.y), arrangementTime);
    }

    // Tween the UI content to a specific size on the Y-axis
    public void TweenToSizeY(float tweenSizeY)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current Y-size is different from the target size
        if (rectTransform.sizeDelta.y != tweenSizeY)
            // Tween the UI content to the target Y-size
            rectTransform.DOSizeDelta(new Vector2(rectTransform.sizeDelta.x, tweenSizeY), arrangementTime);
    }

    // Tween the UI content to a specific scale on the X-axis
    public void TweenToScaleX(float tweenScaleX)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current X-scale is different from the target scale
        if (rectTransform.localScale.x != tweenScaleX)
            // Tween the UI content to the target X-scale
            rectTransform.DOScale(new Vector2(tweenScaleX, rectTransform.localScale.y), arrangementTime);
    }

    // Tween the UI content to a specific scale on the Y-axis
    public void TweenToScaleY(float tweenScaleY)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current Y-scale is different from the target scale
        if (rectTransform.localScale.y != tweenScaleY)
            // Tween the UI content to the target Y-scale
            rectTransform.DOScale(new Vector2(rectTransform.localScale.x, tweenScaleY), arrangementTime);
    }

    // Tween the UI content to a specific scale
    public void TweenToScale(float tweenScale)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the current scale is different from the target scale
        if (rectTransform.localScale != (Vector3.one * tweenScale))
            // Tween the UI content to the target scale
            rectTransform.DOScale(Vector3.one * tweenScale, arrangementTime);
    }

    // Set the scale of the UI content to zero
    public void SetScaleToZero()
    {
        // Check if the RectTransform component is available
        if (rectTransform)
            // Set the scale to zero
            rectTransform.localScale = Vector2.zero;
    }

    // Set the X-position offset of the UI content
    public void SetPositionXOffset(float positionOffset)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the RectTransform component is available
        if (rectTransform)
            // Set the X-position offset
            rectTransform.anchoredPosition = new Vector2(startPos.x + positionOffset, startPos.y);
    }

    // Set the Y-position offset of the UI content
    public void SetPositionYOffset(float positionOffset)
    {
        // Set up the initial state of the UI content
        SetUpMenuContent();

        // Check if the RectTransform component is available
        if (rectTransform)
            // Set the Y-position offset
            rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + positionOffset);
    }

    // Reset the size of the UI content to its initial size with a tween
    public void ResetTweenSize()
    {
        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the UI content should stay up
        if (!stayUp)
            // Reset the size with a tween
            rectTransform.DOSizeDelta(startSize, arrangementTime * 2f);
    }

    // Reset the position of the UI content to its initial position with a tween
    public void ResetTweenPos()
    {
        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the UI content should stay up
        if (!stayUp)
            // Reset the position with a tween
            rectTransform.DOAnchorPos(startPos, arrangementTime * 2f);
    }

    // Reset the scale of the UI content to its initial scale with a tween
    public void ResetTweenScale()
    {
        // Check if the keyboard is being used
        if (onUseKeyboard)
            return;

        // Check if the UI content should stay up
        if (!stayUp)
            // Reset the scale with a tween
            rectTransform.DOScale(startScale, arrangementTime * 2f);
    }

    // Reset the position of the UI content to its initial position
    public void ResetPos()
    {
        // Check if the RectTransform component is available
        if (rectTransform)
            // Reset the position
            rectTransform.anchoredPosition = startPos;
    }

    // Reset the size of the UI content to its initial size
    public void ResetSize()
    {
        // Check if the RectTransform component is available
        if (rectTransform)
            // Reset the size
            rectTransform.sizeDelta = startSize;
    }

    // Reset the scale of the UI content to its initial scale
    public void ResetScale()
    {
        // Check if the RectTransform component is available
        if (rectTransform)
            // Reset the scale
            rectTransform.localScale = startScale;
    }

    // Check if the UI content should stay up
    public void CheckStayUp(bool stay)
    {
        // Set the flag to indicate whether the UI content should stay up
        stayUp = stay;
    }
}