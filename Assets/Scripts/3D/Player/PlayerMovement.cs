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
    [SerializeField] float rotationSpeed = 10f; // how fast the player rotates toward movement

    [Header("Corruption Effects")]
    [Tooltip("Multiplier applied to movement speed while in a corrupted room.")]
    [SerializeField] private float corruptedSpeedMultiplier = 0.6f;

    private bool isInCorruptedRoom = false;

    private Vector2 previousInput = Vector2.zero;
    private Rigidbody rb;
    private bool isGrounded = true;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];
        rb = GetComponent<Rigidbody>();
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

        // World-space movement: X = horizontal, Z = vertical input
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        // Apply corruption speed modifier
        float effectiveSpeed = moveSpeed * (isInCorruptedRoom ? corruptedSpeedMultiplier : 1f);

        Vector3 move = moveDirection * effectiveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        // Rotate the player to face movement direction
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (jumpAction.triggered && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
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