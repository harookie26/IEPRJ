using UnityEngine;
using System.Collections;

public class LevelCameraDefault : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 0.125f;
    public Vector3 offset;

    private Transform player;
    private Camera cam;

    private RoomComponent[] roomsCache;

    [Header("Focus Assist")]
    [Tooltip("Rotate toward the player to keep them centered.")]
    [SerializeField] private bool keepTargetInView = true;

    [Tooltip("Only engage focus when the PLAYER is touching walls (based on PlayerMovement signal).")]
    [SerializeField] private bool focusOnlyWhenPlayerTouchingWalls = true;

    [Tooltip("Angular dead zone around current forward where no correction is applied.")]
    [Range(0f, 30f)]
    [SerializeField] private float deadZoneDegrees = 6f;

    [Tooltip("How fast the camera blends its rotation toward the player when focus assist is active.")]
    [SerializeField] private float focusSlerpSpeed = 10f;

    [Tooltip("Optional explicit reference. If not set, found on the Player at runtime.")]
    [SerializeField] private PlayerMovement playerMovement;

    private void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            if (playerMovement == null)
                playerMovement = playerGO.GetComponent<PlayerMovement>();
        }

        cam = GetComponent<Camera>();

        RefreshRoomsCache();
    }

    public void RefreshRoomsCache()
    {
        roomsCache = Object.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        if (player == null || cam == null)
            return;

        if (roomsCache == null || roomsCache.Length == 0)
            RefreshRoomsCache();

        RoomComponent currentRoom = null;
        if (roomsCache != null)
        {
            for (int i = 0; i < roomsCache.Length; i++)
            {
                var room = roomsCache[i];
                if (room == null) continue;
                if (room.IsPlayerInside)
                {
                    currentRoom = room;
                    break;
                }
            }

            if (currentRoom == null)
            {
                const float containsEpsilon = 0.001f;
                for (int i = 0; i < roomsCache.Length; i++)
                {
                    var room = roomsCache[i];
                    if (room == null) continue;

                    var b = room.Bounds;

                    b.Expand(containsEpsilon);
                    if (b.Contains(player.position))
                    {
                        currentRoom = room;
                        break;
                    }
                }
            }
        }

        Vector3 targetPos = new Vector3(player.position.x, player.position.y, 0) + offset;

        if (currentRoom == null)
        {
            Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
            transform.position = smoothedPos;

            float tilt = CalculateTilt(player.position.z, smoothedPos.z);

            // Focus assist (only when player is touching walls, if enabled)
            Quaternion baseRotation = Quaternion.Euler(tilt, 0, 0);
            Quaternion finalRotation = ApplyFocusAssist(baseRotation, smoothedPos);

            transform.rotation = finalRotation;
            return;
        }

        Bounds bounds = currentRoom.Bounds;

        // Calculate frustum extents at a fixed Z plane (room center)
        float camZ = targetPos.z;
        float roomPlaneZ = bounds.center.z;
        float camToPlaneDist = Mathf.Abs(camZ - roomPlaneZ);

        float halfFovRad = Mathf.Deg2Rad * cam.fieldOfView * 0.5f;
        float frustumHeight = 2.0f * camToPlaneDist * Mathf.Tan(halfFovRad);
        float frustumWidth = frustumHeight * cam.aspect;

        // Account for camera offset in BOTH X and Y when clamping
        float minX = bounds.min.x + frustumWidth / 2f + offset.x;
        float maxX = bounds.max.x - frustumWidth / 2f + offset.x;

        float minY = bounds.min.y + frustumHeight / 2f + offset.y;
        float maxY = bounds.max.y - frustumHeight / 2f + offset.y;

        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);

        Vector3 clampedTarget = new Vector3(clampedX, clampedY, targetPos.z);

        Vector3 smoothed = Vector3.Lerp(transform.position, clampedTarget, smoothSpeed);
        transform.position = smoothed;

        // Camera tilt based on player Z distance
        float tiltAngle = CalculateTilt(player.position.z, smoothed.z);
        Quaternion baseRot = Quaternion.Euler(tiltAngle, 0, 0);
        Quaternion finalRot = ApplyFocusAssist(baseRot, smoothed);

        transform.rotation = finalRot;
    }

    // Apply focus assist by slerping from the base tilt rotation toward a look-at(player) rotation,
    // only when conditions are met (e.g., player touching walls).
    private Quaternion ApplyFocusAssist(Quaternion baseRotation, Vector3 cameraPosition)
    {
        if (!keepTargetInView) return baseRotation;

        bool touchingWallsRequired = focusOnlyWhenPlayerTouchingWalls;
        bool playerTouchingWalls = playerMovement != null && playerMovement.IsTouchingWalls;

        bool assistActive = !touchingWallsRequired || playerTouchingWalls;
        if (!assistActive) return baseRotation;

        Vector3 toTarget = player.position - cameraPosition;
        if (toTarget.sqrMagnitude < 0.0001f) return baseRotation;

        float angleFromForward = Vector3.Angle(baseRotation * Vector3.forward, toTarget);
        if (angleFromForward <= deadZoneDegrees) return baseRotation;

        Quaternion lookAt = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        return Quaternion.Slerp(baseRotation, lookAt, focusSlerpSpeed * Time.deltaTime);
    }

    // Helper to calculate tilt angle based on Z distance
    private float CalculateTilt(float playerZ, float cameraZ)
    {
        // Parameters you can tweak:
        float minTilt = 5f; // looking down when player is close
        float maxTilt = 25f; // looking more forward when player is far
        float minZ = 0f;     // closest Z
        float maxZ = 20f;    // farthest Z

        float t = Mathf.InverseLerp(minZ, maxZ, Mathf.Abs(playerZ - cameraZ));
        return Mathf.Lerp(maxTilt, minTilt, t);
    }

    public Vector3 GetOffset()
    {
        return offset;
    }
}