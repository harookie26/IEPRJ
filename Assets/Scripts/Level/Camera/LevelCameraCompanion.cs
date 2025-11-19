using UnityEngine;

[FoldableInspector]
public class LevelCameraCompanion : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -5);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Room Constraint")]
    [Tooltip("If enabled, the camera position is clamped to the bounds of the room the PLAYER is currently in.")]
    [SerializeField] private bool restrictToPlayerRoomBounds = true;

    [Tooltip("Also clamp Y to keep the camera fully inside the room volume (including ceiling/floor).")]
    [SerializeField] private bool clampYInsideRoomVolume = false;

    [Tooltip("Cached reference to the room the PLAYER is currently inside.")]
    [SerializeField] private RoomComponent playerCurrentRoom;

    [Tooltip("PlayerMovement used to resolve the player's current room. Auto-detected at runtime if not assigned.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Focus Assist")]
    [Tooltip("Keep the paintbrush within view by correcting rotation. Now only engages when CAMERA is touching walls.")]
    [SerializeField] private bool keepTargetInView = true;

    [Tooltip("Angular dead zone around current forward where no correction is applied.")]
    [Range(0f, 30f)]
    [SerializeField] private float deadZoneDegrees = 6f;

    [Tooltip("How fast the camera blends its rotation toward the paintbrush when active.")]
    [SerializeField] private float focusSlerpSpeed = 10f;

    [Header("Ceiling Handling")]
    [Tooltip("When the companion nears the ceiling, automatically lower the camera (in Y) so it doesn't get stuck on the ceiling.")]
    [SerializeField] private bool autoLowerWhenNearCeiling = true;

    [Tooltip("Minimum vertical distance to keep between the camera and the companion when auto-lowering is applied.")]
    [SerializeField] private float minCameraHeightAboveTarget = 0.6f;

    [Tooltip("How close (in meters) the companion must be to the room ceiling to trigger auto-lowering.")]
    [SerializeField] private float ceilingProximityThreshold = 0.25f;

    private Transform target;
    private float rotationX = 0f;
    private float rotationY = 0f;

    // Internal flags based on clamp results this frame
    private bool cameraAgainstBoundary;
    private bool cameraAtCorner;

    private void Start()
    {
        if (target == null)
        {
            GameObject companion = GameObject.FindGameObjectWithTag("Companion");
            if (companion != null)
            {
                target = companion.transform;
            }
            else
            {
                Debug.LogError("No target assigned and no GameObject with 'Companion' tag found!");
                enabled = false;
                return;
            }
        }

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();

        ResolvePlayerRoomAtPosition();

        // Initialize accumulators from current transform to avoid sudden jumps/automatic rotation
        Vector3 e = transform.rotation.eulerAngles;
        rotationX = NormalizeAngle(e.y);
        rotationY = Mathf.Clamp(NormalizeAngle(e.x), minVerticalAngle, maxVerticalAngle);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Keep the cached room up-to-date early so we can adapt thresholds before previewing input
        ResolvePlayerRoomAtPosition();

        // Effective ceiling proximity threshold: hallways are more generous
        float effectiveCeilingThreshold = ceilingProximityThreshold;
        if (restrictToPlayerRoomBounds && playerCurrentRoom != null && playerCurrentRoom.gameObject.CompareTag("Hallway"))
        {
            effectiveCeilingThreshold = 6.0f;
        }

        // Mouse look
        float rawMouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float rawMouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Preview vertical change and cancel it if it would place the camera above the allowed height
        float tentativeRotationY = rotationY + (invertY ? rawMouseY : -rawMouseY);
        tentativeRotationY = Mathf.Clamp(tentativeRotationY, minVerticalAngle, maxVerticalAngle);
        float tentativeRotationX = rotationX + rawMouseX;
        tentativeRotationX = NormalizeAngle(tentativeRotationX);

        Quaternion tentativeRotation = Quaternion.Euler(tentativeRotationY, tentativeRotationX, 0f);
        Vector3 tentativeDesiredPos = target.position + tentativeRotation * offset;

        bool cancelVerticalInput = false;
        if (autoLowerWhenNearCeiling && restrictToPlayerRoomBounds && playerCurrentRoom != null)
        {
            Bounds b = playerCurrentRoom.Bounds;
            float companionToCeiling = b.max.y - target.position.y;
            bool companionNearCeiling = companionToCeiling <= effectiveCeilingThreshold;

            float allowedY = target.position.y + Mathf.Max(minCameraHeightAboveTarget, 0.01f);

            // If companion near ceiling and the tentative desired pos would put camera above allowedY,
            // cancel the vertical input so player cannot push camera further up.
            if (companionNearCeiling && tentativeDesiredPos.y > allowedY)
            {
                cancelVerticalInput = true;
            }
        }

        float mouseX = rawMouseX;
        float mouseY = cancelVerticalInput ? 0f : rawMouseY;

        rotationY += invertY ? mouseY : -mouseY;
        rotationY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);
        rotationX += mouseX;
        rotationX = NormalizeAngle(rotationX);

        Quaternion userRotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Desired camera position from target and offset
        Vector3 desiredPosition = target.position + userRotation * offset;

        // Track pre-smooth clamping (camera tried to go outside right away)
        bool preXClamped = false, preYClamped = false, preZClamped = false;

        if (restrictToPlayerRoomBounds && playerCurrentRoom != null)
        {
            Vector3 unclampedDesired = desiredPosition;
            Vector3 clampedDesired = ClampPositionToRoom(unclampedDesired, playerCurrentRoom.Bounds, clampYInsideRoomVolume);

            preXClamped = !Mathf.Approximately(unclampedDesired.x, clampedDesired.x);
            preYClamped = clampYInsideRoomVolume && !Mathf.Approximately(unclampedDesired.y, clampedDesired.y);
            preZClamped = !Mathf.Approximately(unclampedDesired.z, clampedDesired.z);

            desiredPosition = clampedDesired;

            // Try an early lowering when the companion is very close to the ceiling and the camera is above the companion.
            if (autoLowerWhenNearCeiling)
            {
                Bounds b = playerCurrentRoom.Bounds;
                float companionToCeiling = b.max.y - target.position.y;
                bool companionNearCeiling = companionToCeiling <= effectiveCeilingThreshold;
                bool cameraAboveTarget = unclampedDesired.y > target.position.y + 0.01f;

                if (companionNearCeiling && cameraAboveTarget)
                {
                    float desiredY = target.position.y + Mathf.Max(minCameraHeightAboveTarget, 0.01f);
                    desiredY = Mathf.Clamp(desiredY, b.min.y + 0.01f, b.max.y - 0.01f);
                    desiredPosition.y = desiredY;
                }
            }
        }

        // Smooth toward the (possibly clamped) desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Track post-smooth clamping (lerp overshot outside)
        bool postXClamped = false, postYClamped = false, postZClamped = false;

        if (restrictToPlayerRoomBounds && playerCurrentRoom != null)
        {
            Vector3 unclampedSmoothed = smoothedPosition;
            Vector3 clampedSmoothed = ClampPositionToRoom(unclampedSmoothed, playerCurrentRoom.Bounds, clampYInsideRoomVolume);

            postXClamped = !Mathf.Approximately(unclampedSmoothed.x, clampedSmoothed.x);
            postYClamped = clampYInsideRoomVolume && !Mathf.Approximately(unclampedSmoothed.y, clampedSmoothed.y);
            postZClamped = !Mathf.Approximately(unclampedSmoothed.z, clampedSmoothed.z);

            smoothedPosition = clampedSmoothed;

            // Repeat the auto-lowering for the smoothed position to avoid getting stuck after lerp.
            if (autoLowerWhenNearCeiling)
            {
                Bounds b = playerCurrentRoom.Bounds;
                float companionToCeiling = b.max.y - target.position.y;
                bool companionNearCeiling = companionToCeiling <= effectiveCeilingThreshold;
                bool cameraAboveTarget = unclampedSmoothed.y > target.position.y + 0.01f;

                if (companionNearCeiling && cameraAboveTarget)
                {
                    float desiredY = target.position.y + Mathf.Max(minCameraHeightAboveTarget, 0.01f);
                    desiredY = Mathf.Clamp(desiredY, b.min.y + 0.01f, b.max.y - 0.01f);
                    smoothedPosition.y = desiredY;
                }
            }
        }

        // If the companion is near the ceiling and the camera is above the companion, suppress focus assist
        // because assist would try to rotate the camera to keep the companion centered while the camera can't move.
        bool suppressFocusAssistDueToCeiling = false;
        if (autoLowerWhenNearCeiling && restrictToPlayerRoomBounds && playerCurrentRoom != null)
        {
            Bounds b = playerCurrentRoom.Bounds;
            float companionToCeiling = b.max.y - target.position.y;
            bool companionNearCeiling = companionToCeiling <= effectiveCeilingThreshold;
            bool cameraAboveTarget = smoothedPosition.y > target.position.y + minCameraHeightAboveTarget - 0.001f;
            if (companionNearCeiling && cameraAboveTarget)
            {
                suppressFocusAssistDueToCeiling = true;
            }
        }

        // Aggregate clamp state for this frame
        bool xClamped = preXClamped || postXClamped;
        bool yClamped = preYClamped || postYClamped;
        bool zClamped = preZClamped || postZClamped;

        cameraAgainstBoundary = xClamped || yClamped || zClamped;
        cameraAtCorner = (xClamped ? 1 : 0) + (zClamped ? 1 : 0) >= 2;

        // Focus assist: ONLY when the CAMERA is touching walls (i.e., clamped)
        Quaternion finalRotation = userRotation;
        bool assistActive = keepTargetInView && cameraAgainstBoundary && !suppressFocusAssistDueToCeiling;
        if (assistActive)
        {
            Vector3 toTarget = target.position - smoothedPosition;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float angleFromForward = Vector3.Angle(userRotation * Vector3.forward, toTarget);
                if (angleFromForward > deadZoneDegrees)
                {
                    Quaternion lookAt = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    finalRotation = Quaternion.Slerp(userRotation, lookAt, focusSlerpSpeed * Time.deltaTime);

                    // Keep the yaw/pitch accumulators in sync so mouse look stays consistent after assist
                    Vector3 e = finalRotation.eulerAngles;
                    rotationX = NormalizeAngle(e.y);
                    rotationY = Mathf.Clamp(NormalizeAngle(e.x), minVerticalAngle, maxVerticalAngle);
                }
            }
        }

        transform.position = smoothedPosition;
        transform.rotation = finalRotation;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        Quaternion userRotation = Quaternion.Euler(rotationY, rotationX, 0);
        Vector3 position = target.position + userRotation * offset;

        if (restrictToPlayerRoomBounds && playerCurrentRoom != null)
        {
            position = ClampPositionToRoom(position, playerCurrentRoom.Bounds, clampYInsideRoomVolume);
        }

        // Ensure we look at the target immediately on snap
        Quaternion lookAt = Quaternion.LookRotation((target.position - position).normalized, Vector3.up);
        Vector3 e = lookAt.eulerAngles;
        rotationX = NormalizeAngle(e.y);
        rotationY = Mathf.Clamp(NormalizeAngle(e.x), minVerticalAngle, maxVerticalAngle);

        transform.position = position;
        transform.rotation = lookAt;
    }

    private void ResolvePlayerRoomAtPosition()
    {
        if (playerMovement == null) return;

        Vector3 playerPos = playerMovement.transform.position;

        Collider[] hits = Physics.OverlapSphere(playerPos, 0.05f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var room = hits[i].GetComponent<RoomComponent>();
            if (room != null && room.Bounds.Contains(playerPos))
            {
                playerCurrentRoom = room;
                return;
            }
        }
        // Keep previous reference if none found
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

    private static float NormalizeAngle(float angle)
    {
        // Map [0..360) to (-180..180]
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}