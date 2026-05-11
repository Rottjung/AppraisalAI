using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float turnSpeedDegrees = 360f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private Transform arrow;
    [SerializeField] private float speedMultiplier = 1f;

    public float Speed { get => speed; set => speed = value; }
    public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = Mathf.Max(0f, value); }
    public bool IsMoving => isMoving;

    private Vector3 currentDirection = Vector3.forward;
    private float currentMoveStrength = 0f;
    private bool isMoving = true;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void Move(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();

            float maxRadiansDelta = turnSpeedDegrees * Mathf.Deg2Rad * Time.deltaTime;
            currentDirection = Vector3.RotateTowards(currentDirection, direction, maxRadiansDelta, 0f).normalized;

            currentMoveStrength = Mathf.MoveTowards(currentMoveStrength, 1f, acceleration * Time.deltaTime);
        }
        else
        {
            currentMoveStrength = Mathf.MoveTowards(currentMoveStrength, 0f, acceleration * Time.deltaTime);
        }

        Vector3 velocity = currentDirection * speed * speedMultiplier * currentMoveStrength;
        rb.MovePosition(rb.position + velocity * Time.deltaTime);

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
