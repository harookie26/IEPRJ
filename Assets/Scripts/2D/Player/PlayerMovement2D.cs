using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

public class PlayerMovement2D : MonoBehaviour
{
    private InputSystem2D inputActions;
    private Vector2 moveInput;
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;
    private Rigidbody2D rb;
    private bool isSprinting;
    private bool isChanneling;

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

        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_CHANNELING, PlayerChanneling);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_DECHANNELING, PlayerDechanneling);
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= Move;
        inputActions.Player.Move.canceled -= Move;
        inputActions.Player.Sprint.performed -= OnSprintPerformed;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Disable();

        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_CHANNELING, PlayerChanneling);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_DECHANNELING, PlayerDechanneling);
    }

    private void PlayerChanneling()
    {
        isChanneling = true;
    }

    private void PlayerDechanneling()
    {
        isChanneling = false;
    }

    private void Start()
    {
        isSprinting = false;
        isChanneling = false;
    }

    private void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprinting = true;
        EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STARTED_SPRINT);
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
        EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
    }

    private void FixedUpdate()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        if (moveInput != Vector2.zero)
        {
            if (isChanneling)
            {
                currentSpeed = 0f; // Disable movement while channeling
            }
            rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ResetInput()
    {
        moveInput = Vector2.zero;
    }

    public static void DisableMovement(GameObject player)
    {
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
        {
            movement.enabled = false;
        }
    }
}
