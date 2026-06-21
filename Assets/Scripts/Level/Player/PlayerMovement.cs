using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;
using static EventNames.GameStateEvents;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    private const int MaxWallSlideIterations = 2;
    private const int SafePositionSearchIterations = 6;
    private const float SafePositionRadiusInset = 0.002f;

    [Header("Input References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;

    [Header("Paintbrush References")]
    [SerializeField] private PBController pbController;

    private InputAction moveAction;
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

    [Header("Audio")]
    [SerializeField] private AudioClip footstepAudioClip;
    private AudioSource sfxAudioSource;

    [Header("Footstep Settings")]
    [SerializeField] private float walkStepInterval = 0.4f;
    [SerializeField] private float sprintStepInterval = 0.25f;
    [SerializeField] private float walkPitch = 1.0f;
    [SerializeField] private float sprintPitch = 1.2f;
    private float stepTimer = 0f;

    [Header("Room Constraint")]
    [SerializeField] private bool restrictToRoomBounds = true;
    [SerializeField] private bool clampYInsideRoomVolume = false;
    [SerializeField] private RoomComponent currentRoom;

    [Header("External Control")]
    private bool useExternalMovement = false;
    private Vector2 externalMoveInput;
    private Vector2 externalLookInput;

    [Header("Anti Clipping")]
    [SerializeField] private CapsuleCollider playerCapsule;

    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask groundMask;

    [SerializeField] private float antiClipSkin = 0.04f;
    [SerializeField] private bool preventCornerLift = true;
    [SerializeField] private float maxAllowedCornerLift = 0.03f;
    private Vector3 lastValidPosition;
    private readonly Collider[] overlapResults = new Collider[16];

    private Rigidbody rb;
    private bool canMove = true;

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
    [SerializeField, Range(0f, 89f)] private float maxWalkableSlopeAngle = 55f;

    private float actualHorizontalSpeed;

    public bool IsTouchingWalls { get; private set; }
    public bool IsAtRoomCorner { get; private set; }
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsCurrentlySprinting => isCurrentlySprinting;
    public bool IsExhausted => isExhausted;

    public bool isGamePaused = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = playerInput.actions["Movement"];
        lookAction = playerInput.actions["Look"];

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        collectibleManager = FindFirstObjectByType<PlayerCollectibleManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bodyYaw = transform.eulerAngles.y;
        currentStamina = maxStamina;

        sfxAudioSource = GameObject.FindWithTag("SFXAudioSource").GetComponent<AudioSource>();
        if (sfxAudioSource == null)
            sfxAudioSource = gameObject.AddComponent<AudioSource>();

        if (playerCapsule == null)
        {
            playerCapsule = GetComponent<CapsuleCollider>();
        }

        lastValidPosition = rb.position;
        //rb.maxDepenetrationVelocity = 1.0f;
    }

    private void Start()
    {
        Debug.Log("Current Action Map: " + playerInput.currentActionMap?.name);

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

        // Cutscene-authored external movement may continue, but all player input
        // (including look and sprint) is suppressed for the cutscene duration.
        if (GameState.IsCutsceneActive && !useExternalMovement)
        {
            moveInput = Vector2.zero;
            lookInputTarget = Vector2.zero;
            lookInputCurrent = Vector2.zero;
            cachedMoveDirection = Vector3.zero;
            isCurrentlySprinting = false;
            currentHorizontalVelocity = Vector3.zero;
            return;
        }

        if (!canMove || PBController.IsCompanionManualModeActive)
        {
            moveInput = Vector2.zero;
            lookInputTarget = Vector2.zero;
            cachedMoveDirection = Vector3.zero;
            return;
        }

        if (useExternalMovement)
        {
            moveInput = externalMoveInput;
            lookInputTarget = externalLookInput;
        }
        else
        {
            lookInputTarget = lookAction.ReadValue<Vector2>();
            moveInput = moveAction.ReadValue<Vector2>();
        }

        lookInputCurrent = Vector2.Lerp(lookInputCurrent, lookInputTarget, lookInterpolationSpeed);

        bodyYaw += lookInputCurrent.x * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

        cameraPitch -= lookInputCurrent.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);


        cachedMoveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        HandleFootsteps();
    }

    private void FixedUpdate()
    {
        if (GameState.IsCutsceneActive && !useExternalMovement)
        {
            currentHorizontalVelocity = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (!canMove) return;

        if (isGamePaused) return;

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

        if (isCurrentlySprinting && !isExhausted && actualHorizontalSpeed > 0.1f)
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

        bool isGrounded = TryGetGroundHit(out RaycastHit groundHit);
        bool isOnWalkableGround = isGrounded
            && Vector3.Angle(groundHit.normal, Vector3.up) <= maxWalkableSlopeAngle;

        rb.useGravity = !isOnWalkableGround;

        Vector3 targetVelocity = cachedMoveDirection * effectiveSpeed;

        ApplyAcceleration(ref currentHorizontalVelocity, targetVelocity);

        Vector3 combinedVelocity;

        if (isOnWalkableGround)
        {
            Vector3 horizontalVelocity = new Vector3(
                currentHorizontalVelocity.x,
                0f,
                currentHorizontalVelocity.z
            );

            Vector3 slopeVelocity = Vector3.ProjectOnPlane(
                horizontalVelocity,
                groundHit.normal
            );

            if (slopeVelocity.sqrMagnitude > 0.0001f)
                slopeVelocity = slopeVelocity.normalized * horizontalVelocity.magnitude;

            combinedVelocity = slopeVelocity;
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            combinedVelocity = new Vector3(
                currentHorizontalVelocity.x,
                rb.linearVelocity.y,
                currentHorizontalVelocity.z
            );
        }

        ApplyMovementPhysics(combinedVelocity);
    }

    private void ApplyAcceleration(ref Vector3 currentVel, Vector3 targetVel)
    {
        Vector3 velocityDifference = targetVel - currentVel;
        float distanceToTarget = velocityDifference.magnitude;

        if (distanceToTarget > 0.01f)
        {
            float acceleration = (distanceToTarget > 0) ? moveAcceleration : moveDeceleration;

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
        Vector3 oldPosition = rb.position;

        bool wasGrounded = IsGrounded();

        IsTouchingWalls = false;
        IsAtRoomCorner = false;

        Vector3 desiredDelta =
            velocityWithGravity * Time.fixedDeltaTime;

        desiredDelta =
            ResolveWallSlide(oldPosition, desiredDelta);

        desiredDelta =
            ClampHorizontalDeltaToSafePosition(oldPosition, desiredDelta);

        Vector3 newPos =
            oldPosition + desiredDelta;

        bool shouldClamp =
            restrictToRoomBounds &&
            currentRoom != null &&
            doorwayOverlapCount <= 0;

        if (shouldClamp)
        {
            Vector3 clamped =
                ClampPositionToRoom(
                    newPos,
                    currentRoom.Bounds,
                    clampYInsideRoomVolume
                );

            bool xClamped =
                !Mathf.Approximately(newPos.x, clamped.x);

            bool zClamped =
                !Mathf.Approximately(newPos.z, clamped.z);

            IsTouchingWalls =
                IsTouchingWalls || xClamped || zClamped;

            IsAtRoomCorner =
                xClamped && zClamped;

            if (xClamped)
                currentHorizontalVelocity.x = 0f;

            if (zClamped)
                currentHorizontalVelocity.z = 0f;

            newPos = clamped;
        }

        // Wall contacts must never turn horizontal movement into upward movement.
        if (preventCornerLift && wasGrounded && IsTouchingWalls)
        {
            float maxY =
                oldPosition.y + maxAllowedCornerLift;

            if (newPos.y > maxY || rb.linearVelocity.y > 0f)
            {
                newPos.y = oldPosition.y;

                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    Mathf.Min(0f, rb.linearVelocity.y),
                    rb.linearVelocity.z
                );
            }
        }

        rb.MovePosition(newPos);
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

    public bool TryFindSafeTeleportPosition(
        Vector3 desiredPosition,
        Vector3 searchDirection,
        float searchDistance,
        int searchSteps,
        out Vector3 safePosition)
    {
        safePosition = desiredPosition;
        if (IsPositionSafe(desiredPosition, SafePositionRadiusInset))
        {
            return true;
        }

        searchDirection = Vector3.ProjectOnPlane(searchDirection, Vector3.up).normalized;
        if (searchDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        searchSteps = Mathf.Max(1, searchSteps);
        Vector3 lateralDirection = Vector3.Cross(Vector3.up, searchDirection);
        float lateralSearchDistance = playerCapsule != null
            ? playerCapsule.radius * 2f
            : 1f;

        for (int i = 1; i <= searchSteps; i++)
        {
            float distance = searchDistance * i / searchSteps;
            Vector3 forwardCandidate = desiredPosition + searchDirection * distance;
            if (IsPositionSafe(forwardCandidate, SafePositionRadiusInset))
            {
                safePosition = forwardCandidate;
                return true;
            }

            float lateralDistance = lateralSearchDistance * i / searchSteps;
            Vector3 leftCandidate = forwardCandidate - lateralDirection * lateralDistance;
            if (IsPositionSafe(leftCandidate, SafePositionRadiusInset))
            {
                safePosition = leftCandidate;
                return true;
            }

            Vector3 rightCandidate = forwardCandidate + lateralDirection * lateralDistance;
            if (IsPositionSafe(rightCandidate, SafePositionRadiusInset))
            {
                safePosition = rightCandidate;
                return true;
            }
        }

        Debug.LogWarning(
            $"[PlayerMovement] No collision-free elevator exit found from {desiredPosition} " +
            $"within {searchDistance} units.");
        return false;
    }

    public void TeleportToPose(Vector3 position, Quaternion rotation)
    {
        bool wasMovementEnabled = canMove;

        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        bodyYaw = rotation.eulerAngles.y;

        moveInput = Vector2.zero;
        lookInputTarget = Vector2.zero;
        lookInputCurrent = Vector2.zero;
        cachedMoveDirection = Vector3.zero;

        ResetVelocity();
        canMove = wasMovementEnabled;

        lastValidPosition = position;
        Physics.SyncTransforms();
        ResolveCurrentRoomAtPosition();
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
        if (actualHorizontalSpeed > 0.1f)
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

    public void SetExternalMovement(Vector2 moveInput, Vector2 lookInput)
    {
        useExternalMovement = true;

        externalMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
        externalLookInput = lookInput;
    }

    public void ClearExternalMovement()
    {
        useExternalMovement = false;

        externalMoveInput = Vector2.zero;
        externalLookInput = Vector2.zero;
    }

    public void RotateTowards(Vector3 worldTargetPosition, float rotationSpeed)
    {
        Vector3 dir =
            worldTargetPosition - transform.position;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot =
            Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        bodyYaw = transform.eulerAngles.y;
    }

    // =========================
    // DEFENSIVE MOVEMENT STACK
    // =========================

    private void GetCapsuleWorldPoints(
    Vector3 worldPosition,
    out Vector3 pointA,
    out Vector3 pointB,
    out float radius)
    {
        Vector3 scale = transform.lossyScale;

        Vector3 center =
            worldPosition +
            transform.rotation * Vector3.Scale(playerCapsule.center, scale);

        Vector3 axis;
        float heightScale;
        float radiusScale;

        switch (playerCapsule.direction)
        {
            case 0:
                axis = transform.right;
                heightScale = Mathf.Abs(scale.x);
                radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                break;

            case 2:
                axis = transform.forward;
                heightScale = Mathf.Abs(scale.z);
                radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                break;

            default:
                axis = transform.up;
                heightScale = Mathf.Abs(scale.y);
                radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                break;
        }

        radius = playerCapsule.radius * radiusScale;

        float height = Mathf.Max(
            playerCapsule.height * heightScale,
            radius * 2f
        );

        float halfSegmentLength = Mathf.Max(0f, (height * 0.5f) - radius);

        pointA = center + axis * halfSegmentLength;
        pointB = center - axis * halfSegmentLength;
    }

    private Vector3 PreventClippingWithCapsuleCast(Vector3 startPosition, Vector3 desiredDelta)
    {
        Vector3 horizontalDelta = new Vector3(desiredDelta.x, 0f, desiredDelta.z);

        float distance = horizontalDelta.magnitude;

        if (distance <= 0.0001f)
            return desiredDelta;

        Vector3 direction = horizontalDelta / distance;

        GetCapsuleWorldPoints(
            startPosition,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius
        );

        if (Physics.CapsuleCast(
            pointA,
            pointB,
            radius,
            direction,
            out RaycastHit hit,
            distance + antiClipSkin,
            wallMask,
            QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - antiClipSkin, 0f);

            desiredDelta.x = direction.x * safeDistance;
            desiredDelta.z = direction.z * safeDistance;

            IsTouchingWalls = true;

            currentHorizontalVelocity.x = 0f;
            currentHorizontalVelocity.z = 0f;
        }

        return desiredDelta;
    }

    private bool IsPositionSafe(Vector3 position, float radiusInset = 0f)
    {
        GetCapsuleWorldPoints(
            position,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius
        );

        radius = Mathf.Max(0.01f, radius - radiusInset);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            pointA,
            pointB,
            radius,
            overlapResults,
            wallMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    private Vector3 ClampHorizontalDeltaToSafePosition(Vector3 startPosition, Vector3 desiredDelta)
    {
        Vector3 horizontalDelta = new Vector3(desiredDelta.x, 0f, desiredDelta.z);
        if (horizontalDelta.sqrMagnitude <= 0.00000001f) return desiredDelta;

        Vector3 verticalDelta = new Vector3(0f, desiredDelta.y, 0f);
        Vector3 targetPosition = startPosition + verticalDelta + horizontalDelta;

        if (IsPositionSafe(targetPosition, SafePositionRadiusInset))
            return desiredDelta;

        float safeFraction = 0f;
        float blockedFraction = 1f;

        for (int i = 0; i < SafePositionSearchIterations; i++)
        {
            float testFraction = (safeFraction + blockedFraction) * 0.5f;
            Vector3 testPosition =
                startPosition + verticalDelta + horizontalDelta * testFraction;

            if (IsPositionSafe(testPosition, SafePositionRadiusInset))
                safeFraction = testFraction;
            else
                blockedFraction = testFraction;
        }

        Vector3 safeHorizontalDelta = horizontalDelta * safeFraction;
        desiredDelta.x = safeHorizontalDelta.x;
        desiredDelta.z = safeHorizontalDelta.z;

        currentHorizontalVelocity = safeHorizontalDelta / Time.fixedDeltaTime;
        IsTouchingWalls = true;
        IsAtRoomCorner = true;

        return desiredDelta;
    }

    private void UpdateLastValidPositionOrRecover(Vector3 candidatePosition)
    {
        if (IsPositionSafe(candidatePosition))
        {
            lastValidPosition = candidatePosition;
            return;
        }

        rb.position = lastValidPosition;
        transform.position = lastValidPosition;

        rb.linearVelocity = Vector3.zero;
        currentHorizontalVelocity = Vector3.zero;

        Debug.LogWarning("[AntiClip] Player was inside geometry. Snapped back to last valid position.");
    }

    private Vector3 ResolveWallSlide(Vector3 startPosition, Vector3 desiredDelta)
    {
        Vector3 remainingDelta = new Vector3(
            desiredDelta.x,
            0f,
            desiredDelta.z
        );
        Vector3 resolvedDelta = Vector3.zero;

        GetCapsuleWorldPoints(
            startPosition,
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius
        );

        radius = Mathf.Max(0.01f, radius - antiClipSkin);

        for (int iteration = 0; iteration < MaxWallSlideIterations; iteration++)
        {
            float distance = remainingDelta.magnitude;
            if (distance <= 0.0001f) break;

            Vector3 direction = remainingDelta / distance;
            Vector3 castOffset = resolvedDelta;

            if (!Physics.CapsuleCast(
                pointA + castOffset,
                pointB + castOffset,
                radius,
                direction,
                out RaycastHit hit,
                distance + antiClipSkin,
                wallMask,
                QueryTriggerInteraction.Ignore))
            {
                resolvedDelta += remainingDelta;
                remainingDelta = Vector3.zero;
                break;
            }

            float safeDistance = Mathf.Clamp(
                hit.distance - antiClipSkin,
                0f,
                distance
            );

            Vector3 movementToWall = direction * safeDistance;
            resolvedDelta += movementToWall;
            remainingDelta -= movementToWall;

            Vector3 wallNormal = hit.normal;
            wallNormal.y = 0f;

            if (wallNormal.sqrMagnitude < 0.001f)
                wallNormal = -direction;

            wallNormal.Normalize();
            remainingDelta = Vector3.ProjectOnPlane(remainingDelta, wallNormal);
            remainingDelta.y = 0f;

            IsTouchingWalls = true;
            IsAtRoomCorner |= iteration > 0;

            if (remainingDelta.magnitude < 0.001f)
                break;
        }

        desiredDelta.x = resolvedDelta.x;
        desiredDelta.z = resolvedDelta.z;

        if (IsTouchingWalls)
        {
            currentHorizontalVelocity = new Vector3(
                resolvedDelta.x,
                0f,
                resolvedDelta.z
            ) / Time.fixedDeltaTime;
        }

        return desiredDelta;
    }

    private bool IsGrounded()
    {
        return TryGetGroundHit(out _);
    }

    private bool TryGetGroundHit(out RaycastHit groundHit)
    {
        if (playerCapsule == null)
        {
            groundHit = default;
            return false;
        }

        Vector3 origin = rb.position + Vector3.up * 0.1f;

        float sphereRadius =
            Mathf.Max(0.05f, playerCapsule.radius * 0.85f);

        float castDistance =
            groundDetectionDistance + 0.2f;

        return Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out groundHit,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

}
