using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 3f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.3f;
    [SerializeField] private bool invertY;

    private InputSystem_Actions input;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;

    private void Awake()
    {
        input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled -= ctx => moveInput = Vector2.zero;
        input.Player.Look.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled -= ctx => lookInput = Vector2.zero;
        input.Player.Disable();
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        if (lookInput.sqrMagnitude < 0.001f) return;

        float lookX = lookInput.x * lookSensitivity;
        float lookY = lookInput.y * lookSensitivity * (invertY ? 1f : -1f);

        pitch = Mathf.Clamp(pitch + lookY, -89f, 89f);
        transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y + lookX, 0f);
    }

    private void HandleMove()
    {
        if (moveInput.sqrMagnitude < 0.001f && !Keyboard.current.qKey.isPressed && !Keyboard.current.eKey.isPressed)
            return;

        float speed = moveSpeed;
        if (Keyboard.current.shiftKey.isPressed)
            speed *= sprintMultiplier;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move = (forward * moveInput.y + right * moveInput.x) * speed * Time.deltaTime;

        if (Keyboard.current.qKey.isPressed)
            move += Vector3.down * speed * Time.deltaTime;
        if (Keyboard.current.eKey.isPressed)
            move += Vector3.up * speed * Time.deltaTime;

        transform.position += move;
    }
}
