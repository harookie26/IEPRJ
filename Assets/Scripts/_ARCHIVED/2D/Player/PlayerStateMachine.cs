using UnityEngine;
using static EventNames;
using static EnemyStateMachine;

public class PlayerStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move,
        Sprint,
        Channel,
        Hide,
        Shout
    }

    public State CurrentState { get; private set; } = State.Idle;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("Stamina Settings")]
    public float maxStamina = 5f;
    public float staminaRegenRate = 1f;
    public float staminaRegenDelay = 1f;
    public float staminaLockThreshold = 2f;

    private float currentStamina;
    private float staminaRegenTimer;
    private bool outOfStamina;
    private bool staminaLocked;

    private Rigidbody2D rb;
    private bool canMove = true;
    private bool isChanneling = false;
    private bool isHiding = false;

    // Public read-only properties for animation/controller access
    public bool IsSprinting => CurrentState == State.Sprint;
    public bool OutOfStamina => outOfStamina;
    public bool StaminaLocked => staminaLocked;
    public bool IsHiding => isHiding;

    private EnemyStateMachine enemyStateMachine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        canMove = true;
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_HID, () => isHiding = true);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_REVEALED, () => isHiding = false);
    }

    private void OnDisable()
    {
        canMove = false;

        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_HID, () => isHiding = true);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_REVEALED, () => isHiding = false);
    }

    private void Start()
    {
        currentStamina = maxStamina;
        outOfStamina = false;
        staminaLocked = false;
        ChangeState(State.Idle);

    }

    private void Update()
    {
        if (enemyStateMachine == null)
        {
            GameObject enemy = GameObject.FindWithTag("Enemy");
            if (enemy != null)
                enemyStateMachine = enemy.GetComponent<EnemyStateMachine>();
        }

        if (!canMove)
            return;

        Vector2 moveInput = InputManager.Instance.GetMoveInput();
        bool sprintHeld = InputManager.Instance.IsSprinting();

        // Only allow entering Shout state on the frame the button is pressed
        if (isHiding && CurrentState != State.Shout)
        {
            ChangeState(State.Shout);
        }

        switch (CurrentState)
        {
            case State.Idle:
                HandleIdle(moveInput);
                break;
            case State.Move:
                HandleMove(moveInput, sprintHeld);
                break;
            case State.Sprint:
                HandleSprint(moveInput, sprintHeld);
                break;
            case State.Channel:
                HandleChannel();
                break;
            case State.Shout:
                HandleShout();
                // Immediately return to Idle after shouting
                ChangeState(State.Idle);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveInput = InputManager.Instance.GetMoveInput();

        switch (CurrentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                break;
            case State.Move:
                MovePlayer(moveInput, moveSpeed);
                break;
            case State.Sprint:
                MovePlayer(moveInput, sprintSpeed);
                break;
            case State.Channel:
                rb.linearVelocity = Vector2.zero;
                break;
            case State.Shout:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void MovePlayer(Vector2 moveInput, float speed)
    {
        if (moveInput != Vector2.zero && !isChanneling)
        {
            rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleIdle(Vector2 moveInput)
    {
        if (moveInput != Vector2.zero)
        {
            ChangeState(State.Move);
        }
        HandleStaminaRegen();
    }

    private void HandleMove(Vector2 moveInput, bool sprintHeld)
    {
        if (moveInput == Vector2.zero)
        {
            ChangeState(State.Idle);
            return;
        }
        if (sprintHeld && !outOfStamina && !staminaLocked && currentStamina > 0f)
        {
            ChangeState(State.Sprint);
            return;
        }
        HandleStaminaRegen();
    }

    private void HandleSprint(Vector2 moveInput, bool sprintHeld)
    {
        if (moveInput == Vector2.zero || !sprintHeld)
        {
            if (currentStamina <= staminaLockThreshold)
                staminaLocked = true;
            ChangeState(State.Move);
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
            return;
        }

        bool isUsingStamina = moveInput != Vector2.zero && !isChanneling;
        if (isUsingStamina && currentStamina > 0f)
        {
            currentStamina -= Time.deltaTime;
            staminaRegenTimer = 0f;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                outOfStamina = true;
                staminaLocked = true;
                ChangeState(State.Move);
                EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
            }
        }
        else
        {
            ChangeState(State.Move);
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
        }
    }

    private void HandleChannel()
    {
        // Implement channeling logic if needed
    }

    private void HandleShout()
    {
        Debug.Log("Player shouted while hiding.");
    }

    private void HandleStaminaRegen()
    {
        staminaRegenTimer += Time.deltaTime;
        if (staminaRegenTimer >= staminaRegenDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }
        if (outOfStamina && currentStamina > 0.1f)
            outOfStamina = false;
        if (staminaLocked && currentStamina >= maxStamina)
            staminaLocked = false;
       
    }

    public void ChangeState(State newState)
    {
        if (CurrentState == State.Sprint && newState != State.Sprint)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
        }
        if (newState == State.Sprint)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STARTED_SPRINT);
        }
        CurrentState = newState;
    }

    public float GetCurrentStamina() => currentStamina;
}
