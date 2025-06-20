using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 700f;

    public Vector2 movementInput;
    public Vector2 aimInput;
    public bool CanRotate = true;

    // For modularity, the movement input can be set externally
    public void SetMovementInput(Vector2 input) => movementInput = input;

    public void SetAimInput(Vector2 input) => aimInput = input;
    
    public void SetCanRotate(bool canRotate) => CanRotate = canRotate;

    private void Update()
    {
        Move();
        if (CanRotate)
            Rotate(movementInput);
        else Rotate(aimInput);
    }

    // Handles character movement
    private void Move()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    // Rotates character towards the given direction
    public void Rotate(Vector2 dir)
    {
        if (dir.magnitude < 0.1f) return;

        float rotSpeeed = CanRotate ? rotationSpeed : 100000f;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, angle, 0));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotSpeeed * Time.deltaTime);
    }
}

public enum States
{
    Base,
    Shoot,
    TakingDamage
}