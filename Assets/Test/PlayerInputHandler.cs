using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerInputHandler : MonoBehaviour
{
    static PlayerInputHandler instance;
    public PlayerInput playerInput;
    public FloatingJoystick mobileMoveInput;
    public FloatingJoystick mobileAimInput;
    public Vector2 MovementInput { get; private set; }
    public Vector2 AimDirectionInput { get; private set; }
    public bool IsAimingInput { get; private set; }
    public bool JumpInput { get; private set; }
    public UnityAction InteractAction;
    private Vector2 MoveInput;
    private Vector2 AimInput;
    private bool ActiveAim;

    public bool IsMobile { get { return GameManager.Instance.forcedMobile || MenuManager.Instance.IsWebMobile(); } }
    void Awake()
    {
        instance = this;
        playerInput = GetComponent<PlayerInput>();
    }

    bool hasSetController;
    public void SetInputController()
    {
        mobileMoveInput = GameObject.Find("MobileMoveInput").GetComponent<FloatingJoystick>();
        mobileAimInput = GameObject.Find("MobileAimInput").GetComponent<FloatingJoystick>();
        hasSetController = true;
    }

    private void Update()
    {
        if (!hasSetController) return;

        MovementInput = IsMobile ? GetInputVector(mobileMoveInput.Direction) : GetInputVector(MoveInput);
        AimDirectionInput = IsMobile ? GetInputVector(mobileAimInput.Direction) : GetInputVector(AimInput);
        IsAimingInput = IsMobile ? mobileAimInput.IsActive : ActiveAim;
    }

    public void OnMovementInput(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>(); 
    }

    Vector2 GetInputVector(Vector2 input)
    {
        var forward = Camera.main.transform.forward;
        var right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 tempInputDir = forward * input.y + right * input.x;

        return new Vector2(Mathf.RoundToInt(tempInputDir.x), Mathf.RoundToInt(tempInputDir.z));
    }

    public void OnAimDirectionInput(InputAction.CallbackContext context)
    {
        AimInput = context.ReadValue<Vector2>();
    }

    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started) JumpInput = true;
    }

    public void ResetJumpInput() => JumpInput = false;

    public void OnActionInput(InputAction.CallbackContext context)
    {
        if (context.started) ActiveAim = true;
        else if (context.canceled) ActiveAim = false;
    }

    public void OnInteractInput(InputAction.CallbackContext context)
    {
        if (context.started) InteractAction.Invoke();
    }
}