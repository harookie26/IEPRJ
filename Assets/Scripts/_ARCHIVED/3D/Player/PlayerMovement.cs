using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float mouseSensitivity = 0.1f;

    [Header("Corruption Effects")]
    [SerializeField] private float corruptedSpeedMultiplier = 0.6f;

    [Header("Better Jumping")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Room Constraint")]
    [SerializeField] private bool restrictToRoomBounds = true;
    [SerializeField] private bool clampYInsideRoomVolume = false;
    [SerializeField] private RoomComponent currentRoom;

    private Rigidbody rb;
    private bool isGrounded = true;
    private bool canMove = true;
    private bool jumpRequested = false;
    private bool jumpHeld = false;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float cameraPitch = 0f;
    private float bodyYaw = 0f;

    private int doorwayOverlapCount = 0;

    private PlayerCollectibleManager collectibleManager;
    private const string StairwayTag = "Stairway";

    private Vector3 cachedMoveDirection = Vector3.zero;

    public bool IsTouchingWalls { get; private set; }
    public bool IsAtRoomCorner { get; private set; }

    // 

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];
        lookAction = playerInput.actions["Look"];

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        collectibleManager = FindFirstObjectByType<PlayerCollectibleManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bodyYaw = transform.eulerAngles.y;
    }

    private void Start() => ResolveCurrentRoomAtPosition();

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CATCHED, StopMoving);
        EventBroadcaster.Instance.AddObserver(GameStateEvents.ON_GAME_RESTART, ContinueMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_START, StopMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_END, ContinueMoving);
    }

    private void Update()
    {
        if (!canMove || PBController.IsCompanionManualModeActive)
        {
            moveInput = Vector2.zero;
            cachedMoveDirection = Vector3.zero; // clear it here too
            return;
        }

        lookInput = lookAction.ReadValue<Vector2>();
        moveInput = moveAction.ReadValue<Vector2>();

        bodyYaw += lookInput.x * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // Cache direction AFTER rotation is applied, so FixedUpdate always gets the right one
        cachedMoveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        if (jumpAction.triggered && isGrounded)
            jumpRequested = true;

        jumpHeld = jumpAction.IsPressed();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        bool roomCorrupted = currentRoom != null && currentRoom.isCorrupted;
        bool hasMop = collectibleManager != null && collectibleManager.HasCollected("Mop");
        float effectiveSpeed = moveSpeed * ((roomCorrupted && !hasMop) ? corruptedSpeedMultiplier : 1f);

        // Use the cached direction instead of recalculating from transform 
        Vector3 delta = cachedMoveDirection * effectiveSpeed * Time.fixedDeltaTime;

        ApplyMovementPhysics(delta);
    }

    private void ApplyMovementPhysics(Vector3 delta)
    {
        Vector3 unclampedTarget = rb.position + delta;
        Vector3 targetPos = unclampedTarget;

        bool shouldClamp = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

        IsTouchingWalls = false;
        IsAtRoomCorner = false;

        if (shouldClamp)
        {
            Vector3 clamped = ClampPositionToRoom(unclampedTarget, currentRoom.Bounds, clampYInsideRoomVolume);

            bool xClamped = !Mathf.Approximately(unclampedTarget.x, clamped.x);
            bool zClamped = !Mathf.Approximately(unclampedTarget.z, clamped.z);
            IsTouchingWalls = xClamped || zClamped;
            IsAtRoomCorner = xClamped && zClamped;

            targetPos = clamped;
        }

        rb.MovePosition(targetPos);

        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded = false;
            jumpRequested = false;
        }

        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f) * rb.mass);
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.AddForce(Physics.gravity * (lowJumpMultiplier - 1f) * rb.mass);
    }


    private void OnCollisionEnter(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var room = other.GetComponent<RoomComponent>();
        if (room != null)
        {
            currentRoom = room;
            if (room.BoundsCollider != null && other != room.BoundsCollider) doorwayOverlapCount++;
        }
        else if (other.CompareTag(StairwayTag)) doorwayOverlapCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        var room = other.GetComponent<RoomComponent>();
        if (room != null)
        {
            if (room.BoundsCollider != null && other != room.BoundsCollider)
                doorwayOverlapCount = Mathf.Max(0, doorwayOverlapCount - 1);
        }
        else if (other.CompareTag(StairwayTag)) doorwayOverlapCount = Mathf.Max(0, doorwayOverlapCount - 1);
    }

    private void ResolveCurrentRoomAtPosition()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.05f, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            var room = hit.GetComponent<RoomComponent>();
            if (room != null && room.Bounds.Contains(transform.position))
            {
                currentRoom = room;
                return;
            }
        }
    }

    private static Vector3 ClampPositionToRoom(Vector3 position, Bounds bounds, bool clampY)
    {
        return new Vector3(
            Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
            clampY ? Mathf.Clamp(position.y, bounds.min.y, bounds.max.y) : position.y,
            Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
        );
    }

    private void StopMoving() => canMove = false;
    private void ContinueMoving() => canMove = true;
    public void SetCanMove(bool value) => canMove = value;
}