using System.Collections.Generic;
using UnityEngine;
using static EventNames;

public class CorruptedRoomsManager : MonoBehaviour
{
    public static CorruptedRoomsManager Instance { get; private set; }

    [Header("Global Levitation Defaults (used if randomization disabled)")]
    [Tooltip("Default upward velocity if per-object randomization is OFF.")]
    [SerializeField] private float levitationVelocity = 3f;

    [Tooltip("Default smoothing for non-physics objects if randomization is OFF.")]
    [SerializeField] private float levitationSmooth = 5f;

    [Tooltip("Default rotation speed (deg/sec) if randomization is OFF.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Default height fraction (0..1) if randomization is OFF.")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float levitationHeightFraction = 2f / 3f;

    [Header("Per-Object Randomization")]
    [Tooltip("Enable unique levitation parameters per object.")]
    [SerializeField] private bool usePerObjectRandomization = true;

    [Tooltip("Range for upward velocity (Y push) applied to physics objects.")]
    [SerializeField] private Vector2 upwardVelocityRange = new Vector2(2f, 5f);

    [Tooltip("Range for rotation speed (deg/sec) applied uniformly to all axes.")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(40f, 160f);

    [Tooltip("Range for smoothing (higher = faster) for non-physics objects.")]
    [SerializeField] private Vector2 smoothRange = new Vector2(3f, 9f);

    [Tooltip("Range for levitation height fraction inside room bounds (0.5 .. 0.95).")]
    [SerializeField] private Vector2 heightFractionRange = new Vector2(0.55f, 0.85f);

    [Tooltip("Optional extra vertical noise (added to computed target Y).")]
    [SerializeField] private float verticalJitter = 0.15f;

    // Tracks currently-levitated objects and their original state so we can restore them
    private readonly Dictionary<GameObject, LevitatedData> levitatedObjects = new Dictionary<GameObject, LevitatedData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_TRUE, OnCorruptedRoomMode);
        EventBroadcaster.Instance.AddObserver(LevelEvents.ON_CORRUPTED_ROOM_FALSE, OnNormalRoomMode);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

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

            // Rotate (per-object randomized speed)
            Vector3 perFrameRotation = new Vector3(data.rotationSpeed, data.rotationSpeed, data.rotationSpeed) * Time.deltaTime;
            go.transform.Rotate(perFrameRotation, Space.Self);

            // 3D physics object
            if (data.rb != null)
            {
                data.rb.useGravity = false;

                Vector3 rbPos = data.rb.position;
                float dy = data.targetPosition.y - rbPos.y;

                Vector3 vel = data.rb.linearVelocity;
                if (dy > 0.02f)
                {
                    vel.y = Mathf.Max(vel.y, data.upwardVelocity);
                    data.rb.linearVelocity = vel;
                }
                else
                {
                    vel.y = 0f;
                    data.rb.linearVelocity = vel;
                    Vector3 snap = new Vector3(rbPos.x, data.targetPosition.y, rbPos.z);
                    data.rb.MovePosition(snap);
                }

                continue;
            }

            // 2D physics object
            if (data.rb2d != null)
            {
                data.rb2d.gravityScale = 0f;

                float currentY = data.rb2d.position.y;
                float dy2 = data.targetPosition.y - currentY;

                Vector2 vel2 = data.rb2d.linearVelocity;
                if (dy2 > 0.02f)
                {
                    vel2.y = Mathf.Max(vel2.y, data.upwardVelocity);
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

            // Non-physics object (per-object smoothing)
            float lerpT = 1f - Mathf.Exp(-data.smooth * Time.deltaTime);
            go.transform.position = Vector3.Lerp(go.transform.position, data.targetPosition, lerpT);
        }

        // Cleanup destroyed references
        foreach (var k in toRemove)
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
        var rooms = Object.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
        if (rooms == null || rooms.Length == 0)
        {
            Debug.Log("LevitateObjects: no rooms found.");
            return;
        }

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

            foreach (var go in candidates)
            {
                if (go == null) continue;
                if (levitatedObjects.ContainsKey(go)) continue;
                if (!bounds.Contains(go.transform.position)) continue;

                // Per-object randomized parameters (or fall back to defaults)
                float frac = usePerObjectRandomization
                    ? Clamp01Range(Random.Range(heightFractionRange.x, heightFractionRange.y))
                    : levitationHeightFraction;

                float targetY = bounds.min.y + bounds.size.y * frac;

                float objUpVel = usePerObjectRandomization
                    ? Random.Range(upwardVelocityRange.x, upwardVelocityRange.y)
                    : levitationVelocity;

                float objRotSpeed = usePerObjectRandomization
                    ? Random.Range(rotationSpeedRange.x, rotationSpeedRange.y)
                    : rotationSpeed;

                float objSmooth = usePerObjectRandomization
                    ? Random.Range(smoothRange.x, smoothRange.y)
                    : levitationSmooth;

                float jitter = usePerObjectRandomization && verticalJitter > 0f
                    ? Random.Range(-verticalJitter, verticalJitter)
                    : 0f;

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
                    targetPosition = new Vector3(go.transform.position.x, targetY + jitter, go.transform.position.z),

                    // Randomized per-object parameters
                    upwardVelocity = objUpVel,
                    rotationSpeed = objRotSpeed,
                    smooth = objSmooth,
                    targetHeightFraction = frac
                };

                if (data.rb != null)
                {
                    data.hadGravity = data.rb.useGravity;
                    data.hadKinematic = data.rb.isKinematic;
                    data.rb.useGravity = false;
                    Vector3 vel = data.rb.linearVelocity;
                    vel.y = Mathf.Max(vel.y, data.upwardVelocity);
                    data.rb.linearVelocity = vel;
                }
                else if (data.rb2d != null)
                {
                    data.hadGravity2D = true;
                    data.originalGravityScale2D = data.rb2d.gravityScale;
                    data.rb2d.gravityScale = 0f;
                    Vector2 vel2 = data.rb2d.linearVelocity;
                    vel2.y = Mathf.Max(vel2.y, data.upwardVelocity);
                    data.rb2d.linearVelocity = vel2;
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
            RestoreSingle(go, data);
        }

        int count = levitatedObjects.Count;
        levitatedObjects.Clear();

        Debug.Log($"RestoreLevitatedObjects: restored {count} objects to their original state.");
    }

    public void RestoreRoomAtPosition(Vector3 position)
    {
        var rooms = Object.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
        if (rooms == null || rooms.Length == 0) return;

        RoomComponent targetRoom = null;
        foreach (var room in rooms)
        {
            if (room == null) continue;
            var b = room.Bounds;
            if (b.size == Vector3.zero) continue;
            if (b.Contains(position))
            {
                targetRoom = room;
                break;
            }
        }

        if (targetRoom == null)
        {
            Debug.Log($"RestoreRoomAtPosition: no room found containing position {position}.");
            return;
        }

        Bounds targetBounds = targetRoom.Bounds;
        var toRestore = new List<GameObject>();

        foreach (var kv in levitatedObjects)
        {
            var go = kv.Key;
            var data = kv.Value;
            if (go == null) continue;
            if (targetBounds.Contains(data.originalPosition))
                toRestore.Add(go);
        }

        if (toRestore.Count == 0)
        {
            Debug.Log($"RestoreRoomAtPosition: room '{targetRoom.name}' had no levitated objects to restore.");
            return;
        }

        foreach (var go in toRestore)
        {
            var data = levitatedObjects[go];
            RestoreSingle(go, data);
            levitatedObjects.Remove(go);
        }

        Debug.Log($"RestoreRoomAtPosition: restored {toRestore.Count} objects in room '{targetRoom.name}'. Remaining levitated (other rooms): {levitatedObjects.Count}.");
    }

    private void RestoreSingle(GameObject go, LevitatedData data)
    {
        if (go == null) return;

        if (data.rb != null)
        {
            data.rb.useGravity = data.hadGravity;
            data.rb.isKinematic = data.hadKinematic;
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

        go.transform.position = data.originalPosition;
        go.transform.rotation = data.originalRotation;
    }

    private static float Clamp01Range(float value)
    {
        return Mathf.Clamp(value, 0.5f, 0.95f);
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

        public Vector3 targetPosition;

        // Per-object randomized settings
        public float upwardVelocity;
        public float rotationSpeed;
        public float smooth;
        public float targetHeightFraction;
    }
}   