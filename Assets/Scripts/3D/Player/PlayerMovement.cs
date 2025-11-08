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

    [Header("Room Constraint")]
    [Tooltip("If enabled, player movement is clamped to the current RoomComponent bounds.")]
    [SerializeField] private bool restrictToRoomBounds = true;
    [Tooltip("Also clamp Y to keep the player fully inside the room volume (including ceiling/floor).")]
    [SerializeField] private bool clampYInsideRoomVolume = false;

    [Tooltip("The room whose bounds restrict movement. Auto-detected at runtime when entering a room trigger.")]
    [SerializeField] private RoomComponent currentRoom;

    // Optional: support doorway triggers placed on child GOs with tag "Doorway"
    private const string DoorwayTag = "Doorway";

    private bool isInCorruptedRoom = false;

    private Vector2 previousInput = Vector2.zero;
    private Rigidbody rb;
    private bool isGrounded = true;

    // jump request / hold tracking (handle physics in FixedUpdate)
    private bool jumpRequested = false;
    private bool jumpHeld = false;

    // store desired move direction from Update, applied with MovePosition in FixedUpdate
    private Vector3 currentMoveDirection = Vector3.zero;

    // Count of overlapping doorway triggers; when > 0, allow leaving room bounds
    private int doorwayOverlapCount = 0;

    [Header("Focus Assist (signal for cameras)")]
    [Tooltip("If true, exposes IsTouchingWalls/IsAtRoomCorner so cameras can focus on the player only when the PLAYER is against room walls.")]
    [SerializeField] private bool emitFocusAssistWhenTouchingWalls = true;

    [Tooltip("True when movement this frame was clamped by room bounds on the X or Z axis (doorways ignored).")]
    public bool IsTouchingWalls { get; private set; }

    [Tooltip("True when clamped on multiple lateral axes in the same frame (e.g., a corner).")]
    public bool IsAtRoomCorner { get; private set; }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        ResolveCurrentRoomAtPosition();
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
        if (PBController.IsCompanionManualModeActive)
        {
            previousInput = Vector2.zero;
            currentMoveDirection = Vector3.zero;
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
        currentMoveDirection = moveDirection;

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
        float effectiveSpeed = moveSpeed * (isInCorruptedRoom ? corruptedSpeedMultiplier : 1f);
        Vector3 delta = currentMoveDirection * effectiveSpeed * Time.fixedDeltaTime;

        // Reset focus assist signal each physics step; recompute below
        IsTouchingWalls = false;
        IsAtRoomCorner = false;

        if (rb != null)
        {
            Vector3 basePos = rb.position;
            Vector3 unclampedTarget = basePos + delta;
            Vector3 targetPos = unclampedTarget;

            // Only clamp while NOT overlapping a doorway
            bool shouldClampToRoom = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

            // Track clamp on each axis (similar to LevelCameraCompanion pre-clamp tracking)
            bool preXClamped = false, preYClamped = false, preZClamped = false;

            if (shouldClampToRoom)
            {
                Vector3 clamped = ClampPositionToRoom(unclampedTarget, currentRoom.Bounds, clampYInsideRoomVolume);

                preXClamped = !Mathf.Approximately(unclampedTarget.x, clamped.x);
                preYClamped = clampYInsideRoomVolume && !Mathf.Approximately(unclampedTarget.y, clamped.y);
                preZClamped = !Mathf.Approximately(unclampedTarget.z, clamped.z);

                targetPos = clamped;
            }
            else if (currentRoom != null && clampYInsideRoomVolume)
            {
                targetPos.y = Mathf.Clamp(unclampedTarget.y, currentRoom.Bounds.min.y, currentRoom.Bounds.max.y);
            }

            if (delta.sqrMagnitude > 0f || (restrictToRoomBounds && currentRoom != null))
            {
                rb.MovePosition(targetPos);
            }

            // Jump & better jumping
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

            // Update focus assist signal (only engage for lateral clamping; floors/ceilings excluded)
            if (emitFocusAssistWhenTouchingWalls && shouldClampToRoom)
            {
                bool xClamped = preXClamped;
                bool zClamped = preZClamped;

                IsTouchingWalls = xClamped || zClamped;
                IsAtRoomCorner = (xClamped ? 1 : 0) + (zClamped ? 1 : 0) >= 2;
            }
        }
        else
        {
            Vector3 basePos = transform.position;
            Vector3 unclampedTarget = basePos + delta;
            Vector3 targetPos = unclampedTarget;

            bool shouldClampToRoom = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

            bool preXClamped = false, preYClamped = false, preZClamped = false;

            if (shouldClampToRoom)
            {
                Vector3 clamped = ClampPositionToRoom(unclampedTarget, currentRoom.Bounds, clampYInsideRoomVolume);

                preXClamped = !Mathf.Approximately(unclampedTarget.x, clamped.x);
                preYClamped = clampYInsideRoomVolume && !Mathf.Approximately(unclampedTarget.y, clamped.y);
                preZClamped = !Mathf.Approximately(unclampedTarget.z, clamped.z);

                targetPos = clamped;
            }
            else if (currentRoom != null && clampYInsideRoomVolume)
            {
                targetPos.y = Mathf.Clamp(unclampedTarget.y, currentRoom.Bounds.min.y, currentRoom.Bounds.max.y);
            }

            if (delta.sqrMagnitude > 0f)
            {
                transform.position = targetPos;
            }

            // Update focus assist signal (only engage for lateral clamping; floors/ceilings excluded)
            if (emitFocusAssistWhenTouchingWalls && shouldClampToRoom)
            {
                bool xClamped = preXClamped;
                bool zClamped = preZClamped;

                IsTouchingWalls = xClamped || zClamped;
                IsAtRoomCorner = (xClamped ? 1 : 0) + (zClamped ? 1 : 0) >= 2;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0)
        {
            isGrounded = true;
        }
    }

    // Track the current room and detect doorway overlaps.
    private void OnTriggerEnter(Collider other)
    {
        // 1) Room assignment and doorway detection for colliders on the same GameObject as the room
        var room = other.GetComponent<RoomComponent>();
        if (room != null)
        {
            currentRoom = room;

            // If this trigger collider is NOT the bounds collider, treat as doorway
            var boundsCol = room.BoundsCollider;
            if (boundsCol != null && other != boundsCol)
            {
                doorwayOverlapCount++;
            }
        }
        else
        {
            // 2) Optional: doorway triggers on child objects tagged "Doorway"
            if (other.CompareTag(DoorwayTag))
            {
                doorwayOverlapCount++;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var room = other.GetComponent<RoomComponent>();
        if (room != null)
        {
            var boundsCol = room.BoundsCollider;
            if (boundsCol != null && other != boundsCol)
            {
                doorwayOverlapCount = Mathf.Max(0, doorwayOverlapCount - 1);
            }
        }
        else
        {
            if (other.CompareTag(DoorwayTag))
            {
                doorwayOverlapCount = Mathf.Max(0, doorwayOverlapCount - 1);
            }
        }
    }

    private void ResolveCurrentRoomAtPosition()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.05f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var room = hits[i].GetComponent<RoomComponent>();
            if (room != null)
            {
                if (room.Bounds.Contains(transform.position))
                {
                    currentRoom = room;
                    return;
                }
            }
        }
    }

    private static Vector3 ClampPositionToRoom(Vector3 position, Bounds bounds, bool clampY)
    {
        if (clampY)
        {
            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(position.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
            );
        }
        else
        {
            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                position.y,
                Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
            );
        }
    }
}