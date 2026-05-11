using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames.GameStateEvents;
using static EventNames;
using Unity.VisualScripting;

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
    [SerializeField] float mouseSensitivity = 0.1f;

    [Header("Movement Acceleration")]
    [SerializeField] private float moveAcceleration = 25f;
    [SerializeField] private float moveDeceleration = 20f;

    [Header("Corruption Effects")]
    [SerializeField] private float corruptedSpeedMultiplier = 0.6f;

    [Header("Jumping")]
    [SerializeField] float jumpForce = 2f;
    [SerializeField] private float jumpAccelerationDuration = 0.4f;
    [SerializeField] private AnimationCurve jumpAscentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float jumpAscendMultiplier = 1.5f;
    [SerializeField] private float fallMultiplier = 1.2f;

    [Header("Coyote Time & Air Control")]
    [SerializeField] private float coyoteTimeDuration = 0.2f;
    [SerializeField] private float airStrafeMultiplier = 0.3f;
    [SerializeField] private float landingDeceleration = 30f;
    [SerializeField] private float landingRecoveryDuration = 0.1f;

    [Header("Room Constraint")]
    [SerializeField] private bool restrictToRoomBounds = true;
    [SerializeField] private bool clampYInsideRoomVolume = false;
    [SerializeField] private RoomComponent currentRoom;

    private Rigidbody rb;
    private bool isGrounded = true;
    private bool canMove = true;
    private bool jumpRequested = false;

    private float jumpStartTime = 0f;
    private Vector3 jumpStartVelocity = Vector3.zero;
    private bool isJumpAscending = false;

    private float coyoteTimeRemaining = 0f;
    private bool wasGroundedLastFrame = true;
    private float landingRecoveryTimeRemaining = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 lookInputTarget;
    private Vector2 lookInputCurrent;

    private Vector3 currentHorizontalVelocity = Vector3.zero;

    [Header("Look Interpolation")]
    [SerializeField] private float lookInterpolationSpeed = 0.15f;

    private float cameraPitch = 0f;

    private float bodyYaw = 0f;

    private int doorwayOverlapCount = 0;

    private PlayerCollectibleManager collectibleManager;
    private const string StairwayTag = "Stairway";

    private Vector3 cachedMoveDirection = Vector3.zero;

    [Header("Ground Detection")]
    [SerializeField] private float groundDetectionDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = -1;

    public bool IsTouchingWalls { get; private set; }
    public bool IsAtRoomCorner { get; private set; }
    public bool IsGrounded => isGrounded;

    public bool isGamePaused = false;



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
        coyoteTimeRemaining = 0f;
    }

    private void Start() => ResolveCurrentRoomAtPosition();

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CATCHED, StopMoving);
        EventBroadcaster.Instance.AddObserver(GameStateEvents.ON_GAME_RESTART, ContinueMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_START, StopMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_END, ContinueMoving);
        EventBroadcaster.Instance.AddObserver(ON_GAME_PAUSE, GameIsPaused);
        EventBroadcaster.Instance.AddObserver(ON_GAME_RESUME, GameIsResumed);
    }

    private void Update()
    {
        if(isGamePaused) return; // Prevent processing input when the game is paused

        if (!canMove || PBController.IsCompanionManualModeActive)
        {
            moveInput = Vector2.zero;
            lookInputTarget = Vector2.zero;
            cachedMoveDirection = Vector3.zero;
            return;
        }

        lookInputTarget = lookAction.ReadValue<Vector2>();
        moveInput = moveAction.ReadValue<Vector2>();

        lookInputCurrent = Vector2.Lerp(lookInputCurrent, lookInputTarget, lookInterpolationSpeed);

        bodyYaw += lookInputCurrent.x * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

        cameraPitch -= lookInputCurrent.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);
        

        cachedMoveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        if (jumpAction.triggered && (isGrounded || coyoteTimeRemaining > 0))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        UpdateGroundStatus();

        DetectLanding();
        UpdateCoyoteTime();
        UpdateLandingRecovery();

        bool roomCorrupted = currentRoom != null && currentRoom.isCorrupted;
        bool hasMop = collectibleManager != null && collectibleManager.HasCollected("Mop");
        float effectiveSpeed = moveSpeed * ((roomCorrupted && !hasMop) ? corruptedSpeedMultiplier : 1f);

        Vector3 targetVelocity = cachedMoveDirection * effectiveSpeed;

        ApplyAcceleration(ref currentHorizontalVelocity, targetVelocity);

        Vector3 combinedVelocity = new Vector3(currentHorizontalVelocity.x, rb.linearVelocity.y, currentHorizontalVelocity.z);

        ApplyMovementPhysics(combinedVelocity);
    }

    private void UpdateGroundStatus()
    {
        if (isJumpAscending)
            return;

        Vector3 rayOrigin = rb.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayOrigin, Vector3.down, groundDetectionDistance, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeRemaining = coyoteTimeDuration;
            wasGroundedLastFrame = true;
        }
        else
        {
            coyoteTimeRemaining -= Time.fixedDeltaTime;
            wasGroundedLastFrame = false;
        }
    }

    private void UpdateLandingRecovery()
    {
        if (landingRecoveryTimeRemaining > 0)
        {
            landingRecoveryTimeRemaining -= Time.fixedDeltaTime;
        }
    }

    private void DetectLanding()
    {
        if (!wasGroundedLastFrame && isGrounded)
        {
            landingRecoveryTimeRemaining = landingRecoveryDuration;
            ApplyLandingDeceleration();
        }
        wasGroundedLastFrame = isGrounded;
    }

    private void ApplyLandingDeceleration()
    {
        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, landingDeceleration * Time.fixedDeltaTime);
    }


    private void ApplyAcceleration(ref Vector3 currentVel, Vector3 targetVel)
    {
        Vector3 velocityDifference = targetVel - currentVel;
        float distanceToTarget = velocityDifference.magnitude;

        if (distanceToTarget > 0.01f)
        {
            float acceleration = (distanceToTarget > 0) ? moveAcceleration : moveDeceleration;

            if (!isGrounded)
            {
                acceleration *= airStrafeMultiplier;
            }

            float maxDelta = acceleration * Time.fixedDeltaTime;

            currentVel = Vector3.Lerp(currentVel, targetVel, Mathf.Min(maxDelta / distanceToTarget, 1f));
        }
        else
        {
            currentVel = targetVel;
        }
    }

    private void ApplyMovementPhysics(Vector3 velocityWithGravity)
    {
        Vector3 newPos = rb.position + velocityWithGravity * Time.fixedDeltaTime;

        bool shouldClamp = restrictToRoomBounds && currentRoom != null && doorwayOverlapCount <= 0;

        IsTouchingWalls = false;
        IsAtRoomCorner = false;

        if (shouldClamp)
        {
            Vector3 clamped = ClampPositionToRoom(newPos, currentRoom.Bounds, clampYInsideRoomVolume);

            bool xClamped = !Mathf.Approximately(newPos.x, clamped.x);
            bool zClamped = !Mathf.Approximately(newPos.z, clamped.z);
            IsTouchingWalls = xClamped || zClamped;
            IsAtRoomCorner = xClamped && zClamped;

            if (xClamped)
                currentHorizontalVelocity.x = 0f;
            if (zClamped)
                currentHorizontalVelocity.z = 0f;

            newPos = clamped;
        }

        rb.MovePosition(newPos);

        if (jumpRequested && (isGrounded || coyoteTimeRemaining > 0))
        {
            jumpStartTime = Time.time;
            jumpStartVelocity = rb.linearVelocity;
            isJumpAscending = true;
            isGrounded = false;
            coyoteTimeRemaining = 0f;
            jumpRequested = false;
        }

        if (isJumpAscending)
        {
            float jumpElapsedTime = Time.time - jumpStartTime;
            float jumpProgress = Mathf.Clamp01(jumpElapsedTime / jumpAccelerationDuration);

            float curveValue = jumpAscentCurve.Evaluate(jumpProgress);
            float targetJumpVelocity = jumpForce * curveValue * jumpAscendMultiplier;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, targetJumpVelocity, rb.linearVelocity.z);

            if (jumpProgress >= 1f)
            {
                isJumpAscending = false;
            }
        }

        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f) * rb.mass);
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

    public float CameraPitch => cameraPitch;

    private void GameIsPaused()
    {
        isGamePaused = true;
    }

    private void GameIsResumed()
    {
        isGamePaused = false;

        lookInputTarget = Vector2.zero;
        lookInputCurrent = Vector2.zero;
    }

    /// <summary>
    /// Resets the player's velocity to zero. Used after teleportation to prevent physics artifacts.
    /// </summary>
    public void ResetVelocity()
    {
        rb.linearVelocity = Vector3.zero;
        currentHorizontalVelocity = Vector3.zero;
        isJumpAscending = false;
        jumpRequested = false;
        Debug.Log("[PlayerMovement] Velocity reset after teleportation.");
    }
}