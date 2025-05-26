using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += Move;
        inputActions.Player.Move.canceled += Move;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= Move;
        inputActions.Player.Move.canceled -= Move;
        inputActions.Player.Disable();
    }

    private void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
