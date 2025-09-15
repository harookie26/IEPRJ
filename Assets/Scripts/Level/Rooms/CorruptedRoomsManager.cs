using System.Collections.Generic;
using UnityEngine;
using static EventNames;

public class CorruptedRoomsManager : MonoBehaviour
{
    [Header("Levitation Settings")]
    [Tooltip("Upward velocity applied to Rigidbody / Rigidbody2D while levitating.")]
    [SerializeField] private float levitationVelocity = 3f;

    [Tooltip("Smoothing speed for non-physics objects moving to levitation height.")]
    [SerializeField] private float levitationSmooth = 5f;

    [Tooltip("Rotation speed (degrees/sec) applied on each axis while levitating.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Fraction of the room height above the room floor where objects should float (0..1). " +
             "Use values above 0.5 to keep them above the middle. Default ~0.666 = 2/3.")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float levitationHeightFraction = 2f / 3f;

    // Tracks currently-levitated objects and their original state so we can restore them
    private readonly Dictionary<GameObject, LevitatedData> levitatedObjects = new Dictionary<GameObject, LevitatedData>();

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_TRUE, OnCorruptedRoomMode);
        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_FALSE, OnNormalRoomMode);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(LevelEvents.ON_CORRUPTED_ROOM_TRUE, OnCorruptedRoomMode);
        EventBroadcaster.Instance.RemoveActionAtObserver(LevelEvents.ON_CORRUPTED_ROOM_FALSE, OnNormalRoomMode);
    }

    private void Update()
    {
        if (levitatedObjects.Count == 0) return;

        var toRemove = new List<GameObject>();

        foreach (var kv in levitatedObjects)
        {
            var go = kv.Key;
            var data = kv.Value;

            if (go == null)
            {
                toRemove.Add(go);
                continue;
            }

            // Rotate on all axes while levitating
            Vector3 perFrameRotation = new Vector3(rotationSpeed, rotationSpeed, rotationSpeed) * Time.deltaTime;
            go.transform.Rotate(perFrameRotation, Space.Self);

            // 3D physics object: disable gravity and move toward target Y
            if (data.rb != null)
            {
                data.rb.useGravity = false;

                // If below target, push up; if above target, clamp and zero vertical velocity.
                Vector3 rbPos = data.rb.position;
                float dy = data.targetPosition.y - rbPos.y;

                Vector3 vel = data.rb.linearVelocity;
                if (dy > 0.02f)
                {
                    // Give upward velocity (preserve horizontal)
                    vel.y = Mathf.Max(vel.y, levitationVelocity);
                    data.rb.linearVelocity = vel;
                }
                else
                {
                    // Reached or exceeded target — stop vertical movement and snap to target to avoid jitter
                    vel.y = 0f;
                    data.rb.linearVelocity = vel;
                    Vector3 snap = new Vector3(rbPos.x, data.targetPosition.y, rbPos.z);
                    data.rb.MovePosition(snap);
                }

                continue;
            }

            // 2D physics object: disable gravityScale and move toward target Y
            if (data.rb2d != null)
            {
                data.rb2d.gravityScale = 0f;

                float currentY = data.rb2d.position.y;
                float dy2 = data.targetPosition.y - currentY;

                Vector2 vel2 = data.rb2d.linearVelocity;
                if (dy2 > 0.02f)
                {
                    vel2.y = Mathf.Max(vel2.y, levitationVelocity);
                    data.rb2d.linearVelocity = vel2;
                }
                else
                {
                    vel2.y = 0f;
                    data.rb2d.linearVelocity = vel2;
                    data.rb2d.MovePosition(new Vector2(data.rb2d.position.x, data.targetPosition.y));
                }

                continue;
            }

            // Non-physics object: smoothly move toward levitated target position
            go.transform.position = Vector3.Lerp(go.transform.position, data.targetPosition, 1f - Mathf.Exp(-levitationSmooth * Time.deltaTime));
        }

        // cleanup destroyed objects
        foreach (var k in toRemove)
            if (k != null)
                levitatedObjects.Remove(k);
    }

    private void OnCorruptedRoomMode()
    {
        Debug.Log("Corrupted Room Mode Activated");
        LevitateObjects();
    }

    private void OnNormalRoomMode()
    {
        Debug.Log("Normal Room Mode Activated");
        RestoreLevitatedObjects();
    }

    private void LevitateObjects()
    {
        // Find all rooms
        var rooms = Object.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
        if (rooms == null || rooms.Length == 0)
        {
            Debug.Log("LevitateObjects: no rooms found.");
            return;
        }

        // Collect candidates by tag (avoids scanning all colliders repeatedly)
        GameObject[] candidates;
        try
        {
            candidates = GameObject.FindGameObjectsWithTag("canLevitate");
        }
        catch
        {
            candidates = new GameObject[0];
        }

        int added = 0;

        foreach (var room in rooms)
        {
            if (room == null) continue;

            Bounds bounds = room.Bounds;
            if (bounds.size == Vector3.zero) continue;

            // Compute target Y for this room: floor (min.y) + fraction * roomHeight
            float targetY = bounds.min.y + bounds.size.y * levitationHeightFraction;

            foreach (var go in candidates)
            {
                if (go == null) continue;
                if (levitatedObjects.ContainsKey(go)) continue;

                // Check if the object's position is inside the room bounds (X/Z and Y)
                if (!bounds.Contains(go.transform.position)) continue;

                // Store original state and computed target position (x/z preserved, y = room target)
                var data = new LevitatedData
                {
                    originalPosition = go.transform.position,
                    originalRotation = go.transform.rotation,
                    rb = go.GetComponent<Rigidbody>(),
                    rb2d = go.GetComponent<Rigidbody2D>(),
                    hadGravity = false,
                    hadKinematic = false,
                    hadGravity2D = false,
                    originalGravityScale2D = 0f,
                    targetPosition = new Vector3(go.transform.position.x, targetY, go.transform.position.z)
                };

                if (data.rb != null)
                {
                    data.hadGravity = data.rb.useGravity;
                    data.hadKinematic = data.rb.isKinematic;
                    data.rb.useGravity = false;
                    // Give an initial upward push so it starts moving toward target
                    Vector3 vel = data.rb.linearVelocity;
                    vel.y = Mathf.Max(vel.y, levitationVelocity);
                    data.rb.linearVelocity = vel;
                }
                else if (data.rb2d != null)
                {
                    data.hadGravity2D = true;
                    data.originalGravityScale2D = data.rb2d.gravityScale;
                    data.rb2d.gravityScale = 0f;
                    Vector2 vel2 = data.rb2d.linearVelocity;
                    vel2.y = Mathf.Max(vel2.y, levitationVelocity);
                    data.rb2d.linearVelocity = vel2;
                }
                else
                {
                    // non-physics object: will be moved in Update() toward data.targetPosition
                }

                levitatedObjects.Add(go, data);
                added++;
            }
        }

        Debug.Log($"LevitateObjects: started levitating {added} new objects (total {levitatedObjects.Count}).");
    }

    private void RestoreLevitatedObjects()
    {
        foreach (var kv in levitatedObjects)
        {
            var go = kv.Key;
            var data = kv.Value;
            if (go == null) continue;

            // Restore physics state if present
            if (data.rb != null)
            {
                data.rb.useGravity = data.hadGravity;
                data.rb.isKinematic = data.hadKinematic;
                // zero velocities and snap back to original
                data.rb.linearVelocity = Vector3.zero;
                data.rb.MovePosition(data.originalPosition);
            }

            if (data.rb2d != null)
            {
                if (data.hadGravity2D)
                    data.rb2d.gravityScale = data.originalGravityScale2D;
                data.rb2d.linearVelocity = Vector2.zero;
                data.rb2d.MovePosition(data.originalPosition);
            }

            // Restore transform exactly to original values
            go.transform.position = data.originalPosition;
            go.transform.rotation = data.originalRotation;
        }

        int count = levitatedObjects.Count;
        levitatedObjects.Clear();

        Debug.Log($"RestoreLevitatedObjects: restored {count} objects to their original state.");
    }

    private struct LevitatedData
    {
        public Vector3 originalPosition;
        public Quaternion originalRotation;

        public Rigidbody rb;
        public bool hadGravity;
        public bool hadKinematic;

        public Rigidbody2D rb2d;
        public bool hadGravity2D;
        public float originalGravityScale2D;

        // Where the object should float to (x/z preserved, y = computed from room bounds)
        public Vector3 targetPosition;
    }
}
