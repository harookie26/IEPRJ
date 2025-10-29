using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

[FoldableInspector]
public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;
    InputAction jumpAction;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second.")]
    [SerializeField] float moveSpeed = 5f;

    [Tooltip("Upward force applied when jumping.")]
    [SerializeField] float jumpForce = 5f;

    [Tooltip("Speed of rotation to face movement direction.")]
    [SerializeField] float rotationSpeed = 10f;

    [Tooltip("Multiplier for gravity when falling (makes falls snappier).")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Tooltip("Multiplier for gravity when jump is released early (variable jump height).")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Corruption Effects")]
    [Tooltip("Multiplier applied to movement speed while in a corrupted room.")]
    [SerializeField] private float corruptedSpeedMultiplier = 0.6f;

    [Header("Room Constraint")]
    [Tooltip("If enabled, player movement is clamped to the current RoomComponent bounds.")]
    [SerializeField] private bool restrictToRoomBounds = true;

    [Tooltip("Also clamp Y to keep the player fully inside the room volume (including ceiling/floor).")]
    [SerializeField] private bool clampYInsideRoomVolume = false;

    [Tooltip("The room whose bounds restrict movement. Auto-detected at runtime when entering a room trigger.")]
    [SerializeField] private RoomComponent currentRoom;

    private const string DoorwayTag = "Doorway";

    private bool isInCorruptedRoom = false;

    private Vector2 previousInput = Vector2.zero;
    private Rigidbody rb;
    private bool isGrounded = true;

    private bool jumpRequested = false;
    private bool jumpHeld = false;
    private bool isJumping = false;

    private Vector3 currentMoveDirection = Vector3.zero;

    private int doorwayOverlapCount = 0;

    [Header("Grounding")]
    [Tooltip("Upward offset for ground check ray origin.")]
    [SerializeField] private float groundRayUpOffset = 0.1f;

    [Tooltip("Max distance the ground check ray will search downward.")]
    [SerializeField] private float groundRayDistance = 2.0f;

    [Tooltip("Layers considered as walkable ground.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Tooltip("When true, snap player Y to ground while walking (not jumping, not channeling).")]
    [SerializeField] private bool snapToGroundWhileWalking = true;

    private PlayerStateMachine stateMachine;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];
        rb = GetComponent<Rigidbody>();
        stateMachine = GetComponent<PlayerStateMachine>();

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

        bool isChanneling = stateMachine != null && stateMachine.IsChanneling;
        if (jumpAction.triggered && isGrounded && !isChanneling)
        {
            jumpRequested = true;
        }

        jumpHeld = jumpAction.IsPressed();
    }

    private void FixedUpdate()
    {
        bool isChanneling = stateMachine != null && stateMachine.IsChanneling;

        bool groundHitNow = TryGetGroundY(transform.position, out float currentGroundY);
        if (groundHitNow && !isJumping)
        {
            isGrounded = true;
        }
        else if (!groundHitNow && !isJumping)
        {
            isGrounded = false;
        }

        float effectiveSpeed = moveSpeed * (isInCorruptedRoom ? corruptedSpeedMultiplier : 1f);
        Vector3 delta = currentMoveDirection * effectiveSpeed * Time.fixedDeltaTime;

        if (rb != null)
        {
            Vector3 targetPos = rb.position + delta;

            bool shouldClampToRoom = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

            if (shouldClampToRoom)
            {
                targetPos = ClampPositionToRoom(targetPos, currentRoom.Bounds, clampYInsideRoomVolume);
            }
            else if (currentRoom != null && clampYInsideRoomVolume)
            {
                targetPos.y = Mathf.Clamp(targetPos.y, currentRoom.Bounds.min.y, currentRoom.Bounds.max.y);
            }

            if (snapToGroundWhileWalking && !isChanneling && !isJumping)
            {
                if (TryGetGroundY(targetPos, out float groundY))
                {
                    targetPos.y = groundY;
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    isGrounded = true;
                }
            }

            if (delta.sqrMagnitude > 0f || (restrictToRoomBounds && currentRoom != null) || (snapToGroundWhileWalking && !isChanneling && !isJumping))
            {
                rb.MovePosition(targetPos);
            }

            if (jumpRequested && isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

                isGrounded = false;
                isJumping = true;
                jumpRequested = false;
            }

            if (isJumping && !isChanneling)
            {
                if (rb.linearVelocity.y < 0f)
                {
                    rb.AddForce(Physics.gravity * (fallMultiplier - 1f) * rb.mass);
                }
                else if (rb.linearVelocity.y > 0f && !jumpHeld)
                {
                    rb.AddForce(Physics.gravity * (lowJumpMultiplier - 1f) * rb.mass);
                }
            }

            if (isJumping && TryGetGroundY(rb.position, out float landedGroundY))
            {
                if (rb.linearVelocity.y <= 0f && Mathf.Abs(rb.position.y - landedGroundY) <= 0.05f)
                {
                    isJumping = false;
                    isGrounded = true;
                    rb.MovePosition(new Vector3(rb.position.x, landedGroundY, rb.position.z));
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
            }
        }
        else
        {
            if (delta.sqrMagnitude > 0f || (snapToGroundWhileWalking && !isChanneling && !isJumping))
            {
                Vector3 targetPos = transform.position + delta;

                bool shouldClampToRoom = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

                if (shouldClampToRoom)
                {
                    targetPos = ClampPositionToRoom(targetPos, currentRoom.Bounds, clampYInsideRoomVolume);
                }
                else if (currentRoom != null && clampYInsideRoomVolume)
                {
                    targetPos.y = Mathf.Clamp(targetPos.y, currentRoom.Bounds.min.y, currentRoom.Bounds.max.y);
                }

                if (snapToGroundWhileWalking && !isChanneling && !isJumping)
                {
                    if (TryGetGroundY(targetPos, out float groundY))
                    {
                        targetPos.y = groundY;
                        isGrounded = true;
                    }
                }

                transform.position = targetPos;
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

    private void OnTriggerEnter(Collider other)
    {
        var room = other.GetComponent<RoomComponent>();
        if (room != null)
        {
            currentRoom = room;

            var boundsCol = room.BoundsCollider;
            if (boundsCol != null && other != boundsCol)
            {
                doorwayOverlapCount++;
            }
        }
        else
        {
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

    private bool TryGetGroundY(Vector3 position, out float groundY)
    {
        Vector3 origin = position + Vector3.up * groundRayUpOffset;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            return true;
        }

        groundY = default;
        return false;
    }
}