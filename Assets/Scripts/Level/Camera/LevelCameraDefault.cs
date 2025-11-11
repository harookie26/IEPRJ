using UnityEngine;
using System.Collections;

[FoldableInspector]
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
        roomsCache = FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
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

            // Always rotate to keep the player centered in view (placement unchanged)
            transform.rotation = ComputeCenterLookRotation(smoothedPos);
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

        // Always rotate to keep the player centered in view (placement unchanged)
        transform.rotation = ComputeCenterLookRotation(smoothed);
    }

    // New: Always center the player in the camera's POV with smooth rotation.
    private Quaternion ComputeCenterLookRotation(Vector3 cameraPosition)
    {
        Vector3 toTarget = player != null ? (player.position - cameraPosition) : Vector3.forward;
        if (toTarget.sqrMagnitude < 0.0001f)
            return transform.rotation;

        Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        // Clamp yaw (Y axis) to [-25, 25] degrees, keep pitch from desired and zero roll.
        Vector3 desiredEuler = desired.eulerAngles;
        float signedYaw = Mathf.DeltaAngle(0f, desiredEuler.y);
        float clampedYaw = Mathf.Clamp(signedYaw, -25f, 25f);
        Vector3 targetEuler = new Vector3(desiredEuler.x, clampedYaw, 0f);
        Quaternion target = Quaternion.Euler(targetEuler);

        float t = Mathf.Clamp01(focusSlerpSpeed * Time.deltaTime);
        return Quaternion.Slerp(transform.rotation, target, t);
    }
    public Vector3 GetOffset()
    {
        return offset;
    }
}