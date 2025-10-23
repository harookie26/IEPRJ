using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;
    InputAction jumpAction;

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Corruption Effects")]
    [Tooltip("Multiplier applied to movement speed while in a corrupted room.")]
    [SerializeField] private float corruptedSpeedMultiplier = 0.6f;

    [Header("Better Jumping (tune to taste)")]
    [Tooltip("Multiplier for gravity when falling (makes falls snappier).")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [Tooltip("Multiplier for gravity when jump is released early (variable jump height).")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    private bool isInCorruptedRoom = false;

    private Vector2 previousInput = Vector2.zero;
    private Rigidbody rb;
    private bool isGrounded = true;

    // jump request / hold tracking (handle physics in FixedUpdate)
    private bool jumpRequested = false;
    private bool jumpHeld = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];
        rb = GetComponent<Rigidbody>();

        // Prevent physics from rotating the player due to collisions while still allowing
        // rotation driven by this script. Use |= to preserve any other constraints set in the Inspector.
        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_TRUE, OnCorruptedEnter);
        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_FALSE, OnCorruptedExit);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(LevelEvents.ON_CORRUPTED_ROOM_TRUE, OnCorruptedEnter);
        EventBroadcaster.Instance.RemoveActionAtObserver(LevelEvents.ON_CORRUPTED_ROOM_FALSE, OnCorruptedExit);
    }

    private void OnCorruptedEnter()
    {
        isInCorruptedRoom = true;
    }

    private void OnCorruptedExit()
    {
        isInCorruptedRoom = false;
    }

    private void Update()
    {
        // Prevent movement if companion is in manual mode
        if (PBController.IsCompanionManualModeActive)
        {
            previousInput = Vector2.zero;
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();

        if (input != Vector2.zero)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_MOVED);
        }
        else if (previousInput != Vector2.zero)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED);
        }

        previousInput = input;

        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        float effectiveSpeed = moveSpeed * (isInCorruptedRoom ? corruptedSpeedMultiplier : 1f);

        Vector3 move = moveDirection * effectiveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        if (rb != null)
            rb.angularVelocity = Vector3.zero;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (jumpAction.triggered && isGrounded)
        {
            jumpRequested = true;
        }

        jumpHeld = jumpAction.IsPressed();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            isGrounded = false;
            jumpRequested = false;
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f) * rb.mass);
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.AddForce(Physics.gravity * (lowJumpMultiplier - 1f) * rb.mass);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Simple ground check
        if (collision.contacts.Length > 0)
        {
            isGrounded = true;
        }
    }
}