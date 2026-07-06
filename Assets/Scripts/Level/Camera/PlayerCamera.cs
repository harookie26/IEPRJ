using UnityEngine;
using static EventNames.GameStateEvents;

public class PlayerCamera : MonoBehaviour
{
    private PlayerMovement playerMovement;

    [Header("Motion Settings")]
    [SerializeField] private bool enableMotionEffects = true;

    [Header("Head Bob - Walk")]
    [SerializeField] private float walkBobAmountY = 0.06f;
    [SerializeField] private float walkBobAmountX = 0.03f;

    [Header("Head Bob - Run")]
    [SerializeField] private float runBobAmountY = 0.1f;
    [SerializeField] private float runBobAmountX = 0.05f;

    [Header("Head Bob - Speed")]
    [SerializeField] private float baseBobSpeed = 6f;
    [SerializeField] private float maxBobSpeed = 12f;

    [Header("Rotation Sway")]
    [SerializeField] private float swayRollAmount = 5f;
    [SerializeField] private float swayRollSpeed = 4f;

    [Header("Velocity Thresholds")]
    [SerializeField] private float walkSpeedThreshold = 1f;
    [SerializeField] private float runSpeedThreshold = 4f;

    [Header("Motion Smoothing")]
    [SerializeField] private float motionLerpSpeed = 0.1f;

    [Header("Idle Motion")]
    [SerializeField] private float idleMotionFadeInDelay = 2.5f;
    [SerializeField] private float idleMotionFadeInDuration = 1f;
    [SerializeField] private float idlePositionDriftAmount = 0.02f;
    [SerializeField] private float idleRotationAmount = 1.5f;
    [SerializeField] private float idleMotionSpeed = 0.8f;

    [Header("Field of View")]
    [SerializeField] private float idleFOV = 60f;
    [SerializeField] private float walkFOV = 65f;
    [SerializeField] private float sprintFOV = 75f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    [SerializeField] private float lookAtSpeed = 5f;
    private bool useLookAtOverride = false;
    private Transform lookAtTarget;


    private Vector3 lastSafeEulerAngles = Vector3.zero;

    private Rigidbody playerRigidbody;
    private Vector3 originalPosition;
    private Vector3 currentBobOffset = Vector3.zero;
    private float currentRollSway = 0f;

    private float horizontalVelocity = 0f;
    private float targetHorizontalVelocity = 0f;
    private float bobPhaseX = 0f;
    private float bobPhaseY = 0f;

    private Vector3 lastPlayerPosition = Vector3.zero;
    private float calculatedVelocity = 0f;

    private Camera mainCamera;
    private float targetFOV;
    private float currentFOV;
    private float cutsceneRollOffset;

    private float idleTimeElapsed = 0f;
    private float idleMotionPhaseX = 0f;
    private float idleMotionPhaseY = 0f;
    private float currentIdleMotionFade = 0f;
    private float idleRandomSeed = 0f;
    private Vector3 transitionIdleOffset = Vector3.zero;
    private bool isTransitioningFromIdle = false;
    private float headBobTransitionBlend = 1f;

    private bool isGamePaused = false;

    private float resumeCooldown = 0f;

    public bool EnableMotionEffects
    {
        get => enableMotionEffects;
        set => enableMotionEffects = value;
    }


    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(ON_GAME_PAUSE, GameIsPaused);
        EventBroadcaster.Instance.AddObserver(ON_GAME_RESUME, GameIsResumed);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_PAUSE, GameIsPaused);
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_RESUME, GameIsResumed);
    }

    public void GameIsPaused()
    {
        isGamePaused = true;
    }

    public void GameIsResumed()
    {
        isGamePaused = false;

        resumeCooldown = 0.1f; // ignore camera motion briefly

        if (playerRigidbody != null)
            lastPlayerPosition = playerRigidbody.position;

        if (mainCamera != null)
            currentFOV = mainCamera.fieldOfView;

        horizontalVelocity = 0f;
        targetHorizontalVelocity = 0f;
        calculatedVelocity = 0f;

        currentBobOffset = Vector3.zero;
        currentRollSway = 0f;

        originalPosition = transform.localPosition;
    }

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        playerRigidbody = GetComponentInParent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogError($"PlayerCamera: No Rigidbody found in parent hierarchy. Motion effects disabled. Camera parent: {transform.parent?.gameObject.name ?? "None"}", gameObject);
            enableMotionEffects = false;
        }
        else
        {
            Debug.Log($"PlayerCamera: Found Rigidbody on {playerRigidbody.gameObject.name}", gameObject);
        }

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("PlayerCamera: No Camera component found on this GameObject.", gameObject);
        }

        originalPosition = transform.localPosition;
        lastPlayerPosition = playerRigidbody.position;

        targetFOV = idleFOV;
        currentFOV = mainCamera != null ? mainCamera.fieldOfView : idleFOV;
    }

    private void LateUpdate()
    {
        if (isGamePaused) return;

        if (resumeCooldown > 0f)
        {
            resumeCooldown -= Time.deltaTime;

            lastPlayerPosition = playerRigidbody.position;

            transform.localPosition = originalPosition;
            transform.localRotation = Quaternion.Euler(
                playerMovement != null ? playerMovement.CameraPitch : 0f,
                0f,
                0f
            );

            return;
        }

        /*Vector3 euler = transform.localRotation.eulerAngles;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        float yaw = euler.y > 180f ? euler.y - 360f : euler.y;

        if (!float.IsNaN(pitch) && !float.IsNaN(yaw))
            lastSafeEulerAngles = new Vector3(pitch, yaw, 0f);*/

        if (!enableMotionEffects || playerRigidbody == null)
        {
            ResetMotion();
            return;
        }

        Vector3 positionDelta = playerRigidbody.position - lastPlayerPosition;
        Vector3 horizontalDelta = new Vector3(positionDelta.x, 0, positionDelta.z);

        calculatedVelocity = horizontalDelta.magnitude / Time.deltaTime;
        lastPlayerPosition = playerRigidbody.position;

        targetHorizontalVelocity = calculatedVelocity;
        horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, motionLerpSpeed);

        UpdateBobPhases();
        UpdateIdleMotion();
        UpdateFOV();

        currentBobOffset = CalculateHeadBobOffset();
        currentBobOffset += CalculateIdleOffset();
        currentRollSway = CalculateRollSway();
        currentRollSway += CalculateIdleRotation();

        ApplyMotion();
    }

    private void UpdateBobPhases()
    {
        float currentBobSpeed = GetCurrentBobSpeed();

        if (currentBobSpeed > 0)
        {
            bobPhaseX += Time.deltaTime * currentBobSpeed * 0.5f;
            bobPhaseY += Time.deltaTime * currentBobSpeed;

            bobPhaseX = bobPhaseX % 1f;
            bobPhaseY = bobPhaseY % 1f;
        }
    }

    private void UpdateIdleMotion()
    {
        if (horizontalVelocity < walkSpeedThreshold)
        {
            if (idleTimeElapsed == 0f)
            {
                idleRandomSeed = Random.value * 100f;
                isTransitioningFromIdle = false;
                transitionIdleOffset = Vector3.zero;
            }

            idleTimeElapsed += Time.deltaTime;

            float timeSinceDelay = idleTimeElapsed - idleMotionFadeInDelay;
            if (timeSinceDelay > 0f)
            {
                currentIdleMotionFade = Mathf.Clamp01(timeSinceDelay / idleMotionFadeInDuration);
            }
            else
            {
                currentIdleMotionFade = 0f;
            }

            idleMotionPhaseX += Time.deltaTime * idleMotionSpeed * 0.3f;
            idleMotionPhaseY += Time.deltaTime * idleMotionSpeed * 0.4f;

            idleMotionPhaseX = idleMotionPhaseX % 1f;
            idleMotionPhaseY = idleMotionPhaseY % 1f;
        }
        else
        {
            if (!isTransitioningFromIdle && currentIdleMotionFade > 0f)
            {
                float driftX = Mathf.Sin((idleMotionPhaseX + idleRandomSeed) * Mathf.PI * 2f) * idlePositionDriftAmount;
                float driftY = Mathf.Cos((idleMotionPhaseY + idleRandomSeed * 0.7f) * Mathf.PI * 2f) * (idlePositionDriftAmount * 0.8f);
                transitionIdleOffset = new Vector3(driftX, driftY, 0) * currentIdleMotionFade;

                isTransitioningFromIdle = true;
                headBobTransitionBlend = 0f;
            }

            currentIdleMotionFade = Mathf.Lerp(currentIdleMotionFade, 0f, motionLerpSpeed * 2f);
            headBobTransitionBlend = Mathf.Lerp(headBobTransitionBlend, 1f, motionLerpSpeed * 2f);

            if (currentIdleMotionFade < 0.01f && headBobTransitionBlend > 0.99f)
            {
                idleTimeElapsed = 0f;
                currentIdleMotionFade = 0f;
                idleMotionPhaseX = 0f;
                idleMotionPhaseY = 0f;
                idleRandomSeed = 0f;
                isTransitioningFromIdle = false;
                transitionIdleOffset = Vector3.zero;
                headBobTransitionBlend = 1f;
            }
        }
    }

    private float GetCurrentBobSpeed()
    {
        if (horizontalVelocity < walkSpeedThreshold)
            return 0f;

        // Calculate bob speed dynamically based on current velocity
        // Normalized velocity: 0 at walkSpeedThreshold, 1.0 at runSpeedThreshold, beyond 1.0 when sprinting
        float normalizedVelocity = (horizontalVelocity - walkSpeedThreshold) / (runSpeedThreshold - walkSpeedThreshold);
        normalizedVelocity = Mathf.Clamp01(normalizedVelocity); // Clamp for non-sprinting

        // For sprinting (velocity > runSpeedThreshold), allow speed to exceed clamp
        if (horizontalVelocity > runSpeedThreshold)
        {
            normalizedVelocity = horizontalVelocity / runSpeedThreshold;
        }

        // Interpolate between baseBobSpeed and maxBobSpeed based on normalized velocity
        float dynamicBobSpeed = Mathf.Lerp(baseBobSpeed, maxBobSpeed, normalizedVelocity);
        return dynamicBobSpeed;
    }

    private Vector3 CalculateHeadBobOffset()
    {
        if (horizontalVelocity < walkSpeedThreshold)
            return Vector3.Lerp(currentBobOffset, Vector3.zero, motionLerpSpeed);

        bool isRunning = horizontalVelocity >= runSpeedThreshold;
        float bobAmountY = isRunning ? runBobAmountY : walkBobAmountY;
        float bobAmountX = isRunning ? runBobAmountX : walkBobAmountX;

        float noiseY = Mathf.PerlinNoise(0, bobPhaseY) - 0.5f;
        float noiseX = Mathf.PerlinNoise(bobPhaseX, 0) - 0.5f;

        Vector3 bobOffset = new Vector3(
            noiseX * bobAmountX * 2f,
            noiseY * bobAmountY * 2f,
            0
        );

        // Scale head-bob during idle-to-moving transition to avoid snapping
        bobOffset *= headBobTransitionBlend;

        return bobOffset;
    }

    private Vector3 CalculateIdleOffset()
    {
        if (isTransitioningFromIdle)
        {
            transitionIdleOffset = Vector3.Lerp(transitionIdleOffset, Vector3.zero, motionLerpSpeed * 2f);
            return transitionIdleOffset;
        }

        if (horizontalVelocity >= walkSpeedThreshold || currentIdleMotionFade <= 0f)
            return Vector3.zero;

        float driftX = Mathf.Sin((idleMotionPhaseX + idleRandomSeed) * Mathf.PI * 2f) * idlePositionDriftAmount;
        float driftY = Mathf.Cos((idleMotionPhaseY + idleRandomSeed * 0.7f) * Mathf.PI * 2f) * (idlePositionDriftAmount * 0.8f);

        Vector3 idleOffset = new Vector3(driftX, driftY, 0) * currentIdleMotionFade;

        return idleOffset;
    }

    private float CalculateRollSway()
    {
        if (horizontalVelocity < walkSpeedThreshold)
            return Mathf.Lerp(currentRollSway, 0f, motionLerpSpeed);

        float rollNoise = Mathf.Sin(bobPhaseX * Mathf.PI * 2f);
        Vector3 positionDelta = playerRigidbody.position - lastPlayerPosition;
        float velocityDirection = positionDelta.x > 0 ? 1 : (positionDelta.x < 0 ? -1 : 0);

        return rollNoise * swayRollAmount * velocityDirection * (horizontalVelocity / runSpeedThreshold);
    }

    private float CalculateIdleRotation()
    {
        if (horizontalVelocity >= walkSpeedThreshold || currentIdleMotionFade <= 0f)
            return 0f;

        float idleYaw = Mathf.Sin((idleMotionPhaseX + idleRandomSeed * 0.5f) * Mathf.PI * 2f) * idleRotationAmount;

        return idleYaw * currentIdleMotionFade;
    }

    private void ApplyMotion()
    {
        transform.localPosition = originalPosition + currentBobOffset;

        float pitch = playerMovement != null ? playerMovement.CameraPitch : 0f;
        float safeRoll = Mathf.Clamp(currentRollSway, -90f, 90f);
        if (float.IsNaN(safeRoll)) safeRoll = 0f;
        safeRoll += cutsceneRollOffset;

        Quaternion targetRotation;

        if (useLookAtOverride && lookAtTarget != null)
        {
            Vector3 dir =
                lookAtTarget.position - transform.position;

            Quaternion worldLookRot =
                Quaternion.LookRotation(dir);

            Quaternion localLookRot =
                Quaternion.Inverse(transform.parent.rotation)
                * worldLookRot;

            Vector3 euler = localLookRot.eulerAngles;

            float pitchAngle = euler.x;

            if (pitchAngle > 180f)
                pitchAngle -= 360f;

            targetRotation = Quaternion.Euler(
                pitchAngle,
                0f,
                safeRoll
            );
        }
        else
        {
            pitch =
                playerMovement != null
                ? playerMovement.CameraPitch
                : 0f;

            targetRotation =
                Quaternion.Euler(pitch, 0f, safeRoll);
        }

        transform.localRotation = Quaternion.Euler(pitch, 0f, safeRoll);
    }

    private void ResetMotion()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, motionLerpSpeed * 2f);

        float pitch = playerMovement != null ? playerMovement.CameraPitch : 0f;
        Quaternion targetRotation = Quaternion.Euler(pitch, 0f, cutsceneRollOffset);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, motionLerpSpeed * 2f);

        currentBobOffset = Vector3.zero;
        currentRollSway = 0f;
        idleTimeElapsed = 0f;
        currentIdleMotionFade = 0f;
        idleMotionPhaseX = 0f;
        idleMotionPhaseY = 0f;
        idleRandomSeed = 0f;
        isTransitioningFromIdle = false;
        transitionIdleOffset = Vector3.zero;
        headBobTransitionBlend = 1f;
    }

    public void SetCutsceneRoll(float roll)
    {
        cutsceneRollOffset = roll;
    }

    public void ClearCutsceneRoll()
    {
        cutsceneRollOffset = 0f;
    }

    private void UpdateFOV()
    {
        if (mainCamera == null)
            return;

        if (horizontalVelocity < walkSpeedThreshold)
        {
            targetFOV = idleFOV;
        }
        else if (horizontalVelocity < runSpeedThreshold)
        {
            targetFOV = walkFOV;
        }
        else
        {
            targetFOV = sprintFOV;
        }

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
        mainCamera.fieldOfView = currentFOV;
    }

    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
        useLookAtOverride = true;
    }

    public void ClearLookAtTarget()
    {
        useLookAtOverride = false;
        lookAtTarget = null;
    }
}
