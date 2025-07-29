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

    // Stamina variables
    [Header("Stamina Settings")]
    public float maxStamina = 5f;
    public float staminaRegenRate = 1f; 
    public float staminaRegenDelay = 1f;
    public float staminaLockThreshold = 2f;
    private float currentStamina;
    private float staminaRegenTimer;
    private bool outOfStamina;
    private bool staminaLocked;

    // Public read-only properties for animation/controller access
    public bool IsSprinting => isSprinting;
    public bool OutOfStamina => outOfStamina;
    public bool StaminaLocked => staminaLocked;

    private bool canMove;

    private void Awake()
    {
        inputActions = new InputSystem2D();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        canMove = true;

        if (inputActions == null)
            inputActions = new InputSystem2D();

        inputActions.Player.Enable();
        inputActions.Player.Move.performed += Move;
        inputActions.Player.Move.canceled += Move;
        inputActions.Player.Sprint.performed += OnSprintPerformed;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;

        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, DisableMovement);
        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, EnableMovement);
    }

    private void OnDisable()
    {
        canMove = false;

        inputActions.Player.Move.performed -= Move;
        inputActions.Player.Move.canceled -= Move;
        inputActions.Player.Sprint.performed -= OnSprintPerformed;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Disable();

        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, DisableMovement);
        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, EnableMovement);
    }

    private void Start()
    {
        isSprinting = false;
        isChanneling = false;
        currentStamina = maxStamina;
        outOfStamina = false;
        staminaLocked = false;
    }

    private void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        // Prevent sprinting if stamina is locked or not enough stamina
        if (!outOfStamina && !staminaLocked && currentStamina > 0f)
        {
            isSprinting = true;
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STARTED_SPRINT);
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
        // Check if stamina is low enough to lock sprinting
        if (currentStamina <= staminaLockThreshold)
        {
            staminaLocked = true;
        }
        EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
    }

    private void Update()
    {
        if (canMove)
            HandleStamina();
    }

    private void HandleStamina()
    {
        bool isUsingStamina = isSprinting && moveInput != Vector2.zero && !isChanneling;

        if (isUsingStamina)
        {
            if (currentStamina > 0f)
            {
                currentStamina -= Time.deltaTime;
                staminaRegenTimer = 0f; // Reset regen timer while using stamina
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    outOfStamina = true;
                    isSprinting = false; // Force stop sprinting
                    staminaLocked = true; // Lock when completely out of stamina
                    EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
                }
            }
        }
        else
        {
            staminaRegenTimer += Time.deltaTime;
            if (staminaRegenTimer >= staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
            }

            // Condition to reset outOfStamina should be independent of regeneration
            if (outOfStamina && currentStamina > 0.1f)
            {
                outOfStamina = false;
            }
        }

        // Unlock sprinting only when stamina is full
        if (staminaLocked && currentStamina >= maxStamina)
        {
            staminaLocked = false;
        }
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentSpeed = moveSpeed;

        if (isSprinting && !outOfStamina && !staminaLocked)
        {
            currentSpeed = sprintSpeed;
        }

        if (moveInput != Vector2.zero)
        {
            if (isChanneling)
            {
                currentSpeed = 0f;
            }
            rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void DisableMovement()
    {
        canMove = false;
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }
}
