using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 700f;

    public Vector2 movementInput;

    // For modularity, the movement input can be set externally
    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }

    private void Update()
    {
        Move();
        if (movementInput.magnitude > 0.1f) 
            Rotate(movementInput);
    }

    // Handles character movement
    private void Move()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    // Rotates character towards its movement direction
    public void Rotate(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, angle, 0));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}

public enum States
{
    Base,
    Shoot,
    TakingDamage
}