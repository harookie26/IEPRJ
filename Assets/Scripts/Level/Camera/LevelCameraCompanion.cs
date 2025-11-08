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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationY += invertY ? mouseY : -mouseY;
        rotationY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);
        rotationX += mouseX;

        Quaternion userRotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Desired camera position from target and offset
        Vector3 desiredPosition = target.position + userRotation * offset;

        // Keep the cached room up-to-date with the player's current position
        ResolvePlayerRoomAtPosition();

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
        }

        // Aggregate clamp state for this frame
        bool xClamped = preXClamped || postXClamped;
        bool yClamped = preYClamped || postYClamped;
        bool zClamped = preZClamped || postZClamped;

        cameraAgainstBoundary = xClamped || yClamped || zClamped;
        cameraAtCorner = (xClamped ? 1 : 0) + (zClamped ? 1 : 0) >= 2;

        // Focus assist: ONLY when the CAMERA is touching walls (i.e., clamped)
        Quaternion finalRotation = userRotation;
        bool assistActive = keepTargetInView && cameraAgainstBoundary;
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
                    rotationX = e.y;
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
        rotationX = e.y;
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