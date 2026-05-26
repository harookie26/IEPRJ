using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;
using static EventNames.GameStateEvents;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;

    [Header("Paintbrush References")]
    [SerializeField] private PBController pbController;

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

    [Header("Sprint & Stamina")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float minStaminaToSprint = 5f;
    [SerializeField] private float exhaustedSpeedMultiplier = 0.6f;

    [Header("Jumping")]
    [SerializeField] float jumpForce = 2f;
    [SerializeField] private float jumpAccelerationDuration = 0.4f;
    [SerializeField] private AnimationCurve jumpAscentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float jumpAscendMultiplier = 1.5f;
    [SerializeField] private float fallMultiplier = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip footstepAudioClip;
    private AudioSource sfxAudioSource;

    [Header("Footstep Settings")]
    [SerializeField] private float walkStepInterval = 0.4f;
    [SerializeField] private float sprintStepInterval = 0.25f;
    [SerializeField] private float walkPitch = 1.0f;
    [SerializeField] private float sprintPitch = 1.2f;
    private float stepTimer = 0f;

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

    private float currentStamina = 0f;
    private bool isCurrentlySprinting = false;
    private bool isExhausted = false;
    private float exhaustedRecoveryTimeRemaining = 0f;

    [Header("Look Interpolation")]
    [SerializeField] private float lookInterpolationSpeed = 0.15f;

    private float cameraPitch = 0f;

    private float bodyYaw = 0f;

    private int doorwayOverlapCount = 0;

    private PlayerCollectibleManager collectibleManager;
    private const string StairwayTag = "Stairway";

    private Vector3 cachedMoveDirection = Vector3.zero;

    [SerializeField] private float sprintNoiseThreshold = 0.8f;
    private float sprintTimer = 0f;

    [Header("Ground Detection")]
    [SerializeField] private float groundDetectionDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = -1;

    public bool IsTouchingWalls { get; private set; }
    public bool IsAtRoomCorner { get; private set; }
    public bool IsGrounded => isGrounded;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsCurrentlySprinting => isCurrentlySprinting;
    public bool IsExhausted => isExhausted;

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
        currentStamina = maxStamina;

        sfxAudioSource = GameObject.FindWithTag("SFXAudioSource").GetComponent<AudioSource>();
        if (sfxAudioSource == null)
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        ResolveCurrentRoomAtPosition();

        // Move event subscriptions here so they aren't lost during temporary deactivations
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CATCHED, StopMoving);
        EventBroadcaster.Instance.AddObserver(GameStateEvents.ON_GAME_RESTART, ContinueMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_START, StopMoving);
        EventBroadcaster.Instance.AddObserver(UIEvents.PLAY_DIALOGUE_END, ContinueMoving);
        EventBroadcaster.Instance.AddObserver(ON_GAME_PAUSE, GameIsPaused);
        EventBroadcaster.Instance.AddObserver(ON_GAME_RESUME, GameIsResumed);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_STARTED_SPRINT, OnSprintStarted);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_STOPPED_SPRINT, OnSprintStopped);
    }

    private void OnDestroy()
    {
        // Clean up listeners when the player is actually destroyed (e.g., scene change)
        if (EventBroadcaster.Instance != null)
        {
            EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_CATCHED, StopMoving);
            EventBroadcaster.Instance.RemoveActionAtObserver(GameStateEvents.ON_GAME_RESTART, ContinueMoving);
            EventBroadcaster.Instance.RemoveActionAtObserver(UIEvents.PLAY_DIALOGUE_START, StopMoving);
            EventBroadcaster.Instance.RemoveActionAtObserver(UIEvents.PLAY_DIALOGUE_END, ContinueMoving);
            EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_PAUSE, GameIsPaused);
            EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_RESUME, GameIsResumed);
            EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_STARTED_SPRINT, OnSprintStarted);
            EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_STOPPED_SPRINT, OnSprintStopped);
        }
    }

    private void Update()
    {
        if (isGamePaused) return; // Prevent processing input when the game is paused

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

        HandleFootsteps();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        if (isGamePaused) return;

        UpdateGroundStatus();

        DetectLanding();
        UpdateCoyoteTime();
        UpdateLandingRecovery();
        UpdateStamina();

        bool roomCorrupted = currentRoom != null && currentRoom.isCorrupted;
        bool hasMop = collectibleManager != null && collectibleManager.HasCollected("Mop");
        float effectiveSpeed = moveSpeed * ((roomCorrupted && !hasMop) ? corruptedSpeedMultiplier : 1f);

        if (isExhausted)
        {
            effectiveSpeed *= exhaustedSpeedMultiplier;
        }
        else if (isCurrentlySprinting && currentStamina >= minStaminaToSprint && moveInput.magnitude > 0.1f)
        {
            effectiveSpeed *= sprintSpeedMultiplier;
        }

        if (isCurrentlySprinting && !isExhausted && moveInput.magnitude > 0.1f && isGrounded)
        {
            sprintTimer += Time.fixedDeltaTime;

            if (sprintTimer >= sprintNoiseThreshold)
            {
                TriggerGhostNoise();
                sprintTimer = 0f;
            }
        }
        else
        {
            sprintTimer = 0f;
        }

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

    private void OnSprintStarted()
    {
        isCurrentlySprinting = true;
    }

    private void OnSprintStopped()
    {
        isCurrentlySprinting = false;
    }

    private void UpdateStamina()
    {
        if (isExhausted)
        {
            currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);

            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                isExhausted = false;
            }

            return;
        }

        if (isCurrentlySprinting && currentStamina > 0 && moveInput.magnitude > 0.1f)
        {
            currentStamina -= staminaDrainRate * Time.fixedDeltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
    }

    public float CameraPitch => cameraPitch;

    public void GameIsPaused()
    {
        isGamePaused = true;
    }

    public void GameIsResumed()
    {
        isGamePaused = false;

        lookInputTarget = Vector2.zero;
        lookInputCurrent = Vector2.zero;
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = Vector3.zero;
        currentHorizontalVelocity = Vector3.zero;
        isJumpAscending = false;
        jumpRequested = false;

        // 1. FORCE UNLOCK THE PLAYER: 
        // Just in case PBManual or the death sequence permanently locked your constraints or input
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        canMove = true;

        if (pbController != null)
        {
            pbController.SetMode(PBController.Mode.Follow);
            Debug.Log("[PlayerMovement] Found PBController (Active or Inactive) and forced Follow Mode.");
        }
        else
        {
            Debug.LogError("[PlayerMovement] CRITICAL: Could not find PBController anywhere in the scene!");
        }

        Debug.Log("[PlayerMovement] Velocity reset after teleportation and PB detached.");
    }

    private void TriggerGhostNoise()
    {
        EnemyStateMachine ghost = Object.FindFirstObjectByType<EnemyStateMachine>();

        if (ghost != null)
        {
            ghost.ReactToSprinting(transform.position);
        }
    }

    private void HandleFootsteps()
    {
        // Only play footsteps if grounded and moving
        if (isGrounded && currentHorizontalVelocity.magnitude > 0.1f)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                // Check if we are successfully sprinting
                bool isSprintingNow = isCurrentlySprinting && !isExhausted && currentStamina >= minStaminaToSprint;

                // Set interval and base pitch based on movement state
                float currentInterval = isSprintingNow ? sprintStepInterval : walkStepInterval;
                float basePitch = isSprintingNow ? sprintPitch : walkPitch;

                if (footstepAudioClip != null && sfxAudioSource != null)
                {
                    // Add a tiny bit of randomness (+/- 0.05) so it sounds like real, organic footsteps
                    sfxAudioSource.pitch = basePitch + Random.Range(-0.05f, 0.05f);
                    sfxAudioSource.PlayOneShot(footstepAudioClip);
                }

                stepTimer = currentInterval;
            }
        }
        else
        {
            // Reset timer so the moment we move, a step triggers instantly
            stepTimer = 0f;
        }
    }

    public void LoadSaveData(Vector3 loadedPosition, float loadedYaw, float loadedPitch)
    {
        // 1. Teleport the Rigidbody safely
        rb.position = loadedPosition;
        transform.position = loadedPosition;

        // 2. Restore rotation memory
        bodyYaw = loadedYaw;
        cameraPitch = loadedPitch;

        // 3. Apply the horizontal body rotation immediately
        transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

        // 4. Reset velocities so they don't carry falling momentum from before the load
        ResetVelocity();
    }
}