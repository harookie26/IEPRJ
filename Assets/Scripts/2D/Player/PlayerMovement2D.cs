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
    public float maxStamina = 5f; // Maximum stamina in seconds
    public float staminaRegenRate = 1f; // Stamina regenerated per second when not sprinting
    public float staminaRegenDelay = 1f; // Delay before regeneration starts
    public float staminaLockThreshold = 2f; // Stamina level below which sprinting is locked
    private float currentStamina;
    private float staminaRegenTimer;
    private bool outOfStamina;
    private bool staminaLocked; // Prevents sprinting until full

    // Public read-only properties for animation/controller access
    public bool IsSprinting => isSprinting;
    public bool OutOfStamina => outOfStamina;
    public bool StaminaLocked => staminaLocked;

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
        float currentSpeed = moveSpeed;

        // Only allow sprinting if not out of stamina and not locked
        if (isSprinting && !outOfStamina && !staminaLocked)
        {
            currentSpeed = sprintSpeed;
        }

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

    public float GetCurrentStamina()
    {
        return currentStamina;
    }
}
