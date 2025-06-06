using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[SerializeField] GameObject cameraHolder;
	[SerializeField] PlayerInputHandler playerInputHandler;

	[SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
	
	float verticalLookRotation;
	bool grounded;
	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;
	Rigidbody rb;
    public Transform groundCheck;
    public float groundCheckRadius; 
    public LayerMask whatIsGround;

	void Start()
	{
        rb = GetComponent<Rigidbody>();
	}

	void Update()
	{
		Move();
		Jump();

		Cursor.lockState = CursorLockMode.Locked;

		if(transform.position.y < -10f) // Die if you fall out of the world
		{ 
			transform.position = Vector3.zero;
		}
	}
	
	void FixedUpdate()
	{
		rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);

        grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);
	}

	void LateUpdate()
	{
		Look();
	}

	void Look()
	{
		transform.Rotate(Vector3.up * playerInputHandler.AimDirectionInput.x * mouseSensitivity);

		verticalLookRotation -= playerInputHandler.AimDirectionInput.y * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}

	void Move()
	{
		Vector3 moveDir = new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y).normalized;

		moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (playerInputHandler.IsAimingInput ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
	}

	void Jump()
	{
		if(playerInputHandler.JumpInput && grounded)
		{
			rb.AddForce(transform.up * jumpForce);
			playerInputHandler.ResetJumpInput();
		}
	}

	public void SetGroundedState(bool _grounded)
	{
		grounded = _grounded;
	}
}