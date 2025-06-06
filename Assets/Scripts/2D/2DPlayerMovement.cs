using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    private InputSystem2D inputActions;
    private Vector2 moveInput;
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f; // Add a sprint speed
    private Rigidbody2D rb;
    private bool isSprinting = false; // Track sprint state

    private void Awake()
    {
        inputActions = new InputSystem2D();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += Move;
        inputActions.Player.Move.canceled += Move;
        inputActions.Player.Sprint.performed += OnSprintPerformed;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= Move;
        inputActions.Player.Move.canceled -= Move;
        inputActions.Player.Sprint.performed -= OnSprintPerformed;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Disable();
    }

    private void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    private void FixedUpdate()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        if (moveInput != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // This stops all sliding when no input is present
        }
    }

    public void ResetInput()
    {
        moveInput = Vector2.zero;
    }

}
