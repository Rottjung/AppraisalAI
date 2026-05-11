using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float turnSpeedDegrees = 360f; // degrees per second
    [SerializeField] private float acceleration = 8f;       // how fast movement strength ramps up/down
    [SerializeField] private Transform arrow;
    public bool IsMoving => isMoving;

    private Vector3 currentDirection = Vector3.forward;
    private float currentMoveStrength = 0f;
    private bool isMoving = true;

    public void Move(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();

            // Smoothly rotate toward the new target direction.
            float maxRadiansDelta = turnSpeedDegrees * Mathf.Deg2Rad * Time.deltaTime;
            currentDirection = Vector3.RotateTowards(currentDirection, direction, maxRadiansDelta, 0f).normalized;

            // Smoothly ramp movement strength up.
            currentMoveStrength = Mathf.MoveTowards(currentMoveStrength, 1f, acceleration * Time.deltaTime);
        }
        else
        {
            // Smoothly ramp movement strength down if no valid input.
            currentMoveStrength = Mathf.MoveTowards(currentMoveStrength, 0f, acceleration * Time.deltaTime);
        }

        Vector3 velocity = currentDirection * speed * currentMoveStrength;
        transform.position += velocity * Time.deltaTime;

        if (arrow && currentDirection.sqrMagnitude > 0.0001f)
        {
            arrow.rotation = Quaternion.LookRotation(currentDirection);
        }
    }

    public void Stop()
    {
        currentMoveStrength = 0f;
    }
}