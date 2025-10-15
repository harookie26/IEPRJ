using UnityEngine;
using System.Collections;

public class LevelCameraDefault : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 0.125f;
    public Vector3 offset;

    private Transform player;
    private Camera cam;

    // Cache rooms to avoid expensive per-frame FindObjects calls (fixes hitching when crossing room triggers)
    private RoomComponent[] roomsCache;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        cam = GetComponent<Camera>();

        RefreshRoomsCache();
    }

    // Public helper to refresh the room cache if rooms are added/removed at runtime
    public void RefreshRoomsCache()
    {
        // Single allocation / query instead of doing this every LateUpdate
        roomsCache = Object.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        if (player == null || cam == null)
            return;

        // Ensure cache exists (defensive)
        if (roomsCache == null || roomsCache.Length == 0)
            RefreshRoomsCache();

        // Find the current room the player is in (iterate cached array - no per-frame object search)
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
        }

        // Target position (player X/Y + offset, fixed Z)
        Vector3 targetPos = new Vector3(player.position.x, player.position.y, 0) + offset;

        if (currentRoom == null)
        {
            Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
            transform.position = smoothedPos;

            // Camera tilt based on player Z distance
            float tilt = CalculateTilt(player.position.z, smoothedPos.z);
            transform.rotation = Quaternion.Euler(tilt, 0, 0);
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

        float minX = bounds.min.x + frustumWidth / 2f;
        float maxX = bounds.max.x - frustumWidth / 2f;
        // Calculate the clamped Y range, but bias it by the offset
        float minY = bounds.min.y + frustumHeight / 2f + offset.y;
        float maxY = bounds.max.y - frustumHeight / 2f + offset.y;

        // Clamp the camera's Y position using the biased range
        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);

        Vector3 clampedTarget = new Vector3(clampedX, clampedY, targetPos.z);

        Vector3 smoothed = Vector3.Lerp(transform.position, clampedTarget, smoothSpeed);
        transform.position = smoothed;

        // Camera tilt based on player Z distance
        float tiltAngle = CalculateTilt(player.position.z, smoothed.z);
        transform.rotation = Quaternion.Euler(tiltAngle, 0, 0);
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