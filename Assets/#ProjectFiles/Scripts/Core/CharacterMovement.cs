using UnityEngine;
using UnityEngine.AI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    CharacterController controller;
    NavMeshAgent agent;
    public float moveSpeed = 5f;
    public float rotationSpeed = 700f;

    public Vector2 movementInput;
    public Vector3 moveDestination;
    public Vector2 aimInput;
    public bool IsAI;
    public bool CanRotate = true;
    private float stopDistance;
    private float yPos = 0f;

    // For modularity, the movement input can be set externally
    public void SetMovementInput(Vector2 input) => movementInput = input;
    public void SetMoveDestination(Vector3 destination) => moveDestination = destination;
    public void SetStopDistance(float distance) => stopDistance = distance;
    public void SetAimInput(Vector2 input) => aimInput = input;
    public void SetCanRotate(bool canRotate) => CanRotate = canRotate;
    void Start()
    {
        if (IsAI)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
        }
        else controller = GetComponent<CharacterController>();

        yPos = transform.position.y;
    }

    private void Update()
    {
        if (!IsAI)
        {
            Move();

            if (CanRotate) Rotate(movementInput);
            else Rotate(aimInput);
        }
        else AIMove();
    }

    // Handles character movement
    private void Move()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        controller.Move(moveSpeed * Time.deltaTime * moveDirection);
        transform.position = new Vector3(transform.position.x, yPos, transform.position.z);
        // transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void AIMove()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = stopDistance;

        // ONLY recalculate the path if the target destination has moved significantly
        // This stops the NavMeshAgent from micro-stuttering every frame.
        if (Vector3.Distance(agent.destination, moveDestination) > 0.5f)
        {
            agent.SetDestination(moveDestination);
        }

        // If CanRotate is true, look where we are going. If false, look at the aim input.
        Vector3 lookTarget = CanRotate ? moveDestination : transform.position + new Vector3(aimInput.x, 0, aimInput.y);
        
        Vector3 targetDir = (lookTarget - transform.position).normalized;
        targetDir.y = 0; // Keep it horizontal
        
        if (targetDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime / 100f);
        }
    }

    Vector3 currentLookAt;

    // Rotates character towards the given direction
    public void Rotate(Vector2 dir)
    {
        if (dir.magnitude < 0.1f) return;

        float rotSpeeed = CanRotate ? rotationSpeed : 100000f;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, angle, 0));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotSpeeed * Time.deltaTime);
    }

    public Vector3 GetVelocity() 
    {
        if (IsAI && agent != null) return agent.velocity;
        if (!IsAI && controller != null) return controller.velocity;
        return Vector3.zero;
    }
}

public enum States
{
    Base,
    Shoot,
    Ability,
    TakingDamage
}