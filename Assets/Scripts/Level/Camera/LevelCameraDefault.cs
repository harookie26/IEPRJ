using UnityEngine;
using System.Collections;

[FoldableInspector]
public class LevelCameraDefault : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Dynamic Offsets")]
    [Tooltip("Base offset used for normal rooms.")]
    [SerializeField] private Vector3 defaultOffset;
    [Tooltip("Alternative offset applied when inside a room tagged 'Hallway'.")]
    [SerializeField] private Vector3 hallwayOffset;
    [Tooltip("Tag used to identify hallway rooms.")]
    [SerializeField] private string hallwayTag = "Hallway";

    private Transform player;
    private Camera cam;

    private RoomComponent[] roomsCache;
    private RoomComponent previousRoom;

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

    [Header("Zoom Settings")]
    [Tooltip("Minimum field of view when zooming in.")] [SerializeField] private float minFov = 30f;
    [Tooltip("Maximum field of view when zooming out.")] [SerializeField] private float maxFov = 80f;
    [Tooltip("FOV change applied per zoom step.")] [SerializeField] private float zoomStep = 5f;
    [Tooltip("Interpolation speed for smoothing FOV changes.")] [SerializeField] private float fovLerpSpeed = 8f;
    private float targetFov; // Desired FOV we lerp toward

    // --- TEMP FOCUS OVERRIDE ---
    private Transform focusTarget; // current subject camera centers on (defaults to _player)
    private Coroutine focusOverrideRoutine;
    [Header("Enemy Focus Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [Tooltip("Duration (seconds) to stay focused on enemy after zoom in. 0 = until manually reverted.")]
    [SerializeField] private float enemyFocusDurationOnZoomIn = 2f;
    private Transform lastEnemyFocus; // cache last targeted _enemy

    [Header("Enemy Focus Offset")]
    [Tooltip("Offset applied to camera position while focusing on an enemy. If zero, falls back to normal offset.")]
    [SerializeField] private Vector3 enemyFocusOffset = Vector3.zero;

    // --- CURRENT ROOM (cached) ---
    [Header("Room Tracking")]
    [Tooltip("Cached reference to the room the PLAYER is currently inside.")]
    [SerializeField] private RoomComponent currentRoom; // updated each LateUpdate

    // --- BLACKOUT EFFECT ---
    [Header("Room Blackout")]
    [Tooltip("If true, pixels outside the current room bounds are rendered as black.")]
    [SerializeField] private bool blackoutOutsideRoom = false;
    [Tooltip("Material using the blackout shader (Hidden/RoomBoundsBlackout).")]
    [SerializeField] private Material blackoutMaterial;
    [Tooltip("Soft margin fade distance outside bounds. 0 for hard edge.")]
    [SerializeField] private float blackoutSoftMargin = 0f;
    [Tooltip("Shrink vertical bounds to a thin slice around player height for blackout (prevents showing floors above/below).")]
    [SerializeField] private bool shrinkVerticalToPlayerLevel = true;
    [Tooltip("Thickness in meters of the vertical slice kept when shrinking.")]
    [SerializeField] private float verticalThickness = 3f;

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
        if (cam != null)
            targetFov = cam.fieldOfView; // initialize target

        focusTarget = player; // default subject

        // Initialize offsets: if defaultOffset unset (zero vector) but current offset not zero, capture it.
        if (defaultOffset == Vector3.zero)
            defaultOffset = offset;
        else
            offset = defaultOffset; // ensure starting offset consistent

        RefreshRoomsCache();

        // Ensure depth texture for blackout effect
        if (blackoutOutsideRoom && blackoutMaterial != null && cam != null)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            ZoomIn();
            FocusOnEnemy(enemyFocusDurationOnZoomIn);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            ZoomOut();
            RevertFocusToPlayer();
        }

        // Ensure depth texture is enabled if blackout toggled on at runtime
        if (blackoutOutsideRoom && blackoutMaterial != null && cam != null && (cam.depthTextureMode & DepthTextureMode.Depth) == 0)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }

    public void RefreshRoomsCache()
    {
        roomsCache = FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        if (player == null || cam == null)
            return;

        // Resolve current room from _player position (fast overlap) similar to companion camera.
        ResolvePlayerRoomAtPosition();

        if (roomsCache == null || roomsCache.Length == 0)
            RefreshRoomsCache();

        // Fallback legacy detection if direct resolve failed.
        if (currentRoom == null && roomsCache != null)
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

        // Detect room change and adjust offset accordingly.
        if (currentRoom != previousRoom)
        {
            OnRoomChanged(previousRoom, currentRoom);
            previousRoom = currentRoom;
        }

        Transform subject = focusTarget != null ? focusTarget : player;
        Vector3 subjectPos = subject.position;
        Vector3 targetPos = new Vector3(subjectPos.x, subjectPos.y, 0) + offset;

        // If we are focusing on an _enemy (not the _player) use _enemy-specific offset (if provided) and bypass room clamping.
        if (!IsFocusingOnPlayer() && focusTarget == lastEnemyFocus && lastEnemyFocus != null)
        {
            Vector3 effectiveEnemyOffset = enemyFocusOffset == Vector3.zero ? offset : enemyFocusOffset;
            Vector3 enemyTargetPos = new Vector3(subjectPos.x, subjectPos.y, 0) + effectiveEnemyOffset;
            Vector3 smoothedEnemy = Vector3.Lerp(transform.position, enemyTargetPos, smoothSpeed);
            transform.position = smoothedEnemy;
            transform.rotation = ComputeCenterLookRotation(smoothedEnemy, subject);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Mathf.Clamp01(fovLerpSpeed * Time.deltaTime));
            return;
        }

        if (currentRoom == null)
        {
            Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
            transform.position = smoothedPos;
            transform.rotation = ComputeCenterLookRotation(smoothedPos, subject);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Mathf.Clamp01(fovLerpSpeed * Time.deltaTime));
            return;
        }

        Bounds bounds = currentRoom.Bounds;

        float camZ = targetPos.z;
        float roomPlaneZ = bounds.center.z;
        float camToPlaneDist = Mathf.Abs(camZ - roomPlaneZ);

        float halfFovRad = Mathf.Deg2Rad * cam.fieldOfView * 0.5f;
        float frustumHeight = 2.0f * camToPlaneDist * Mathf.Tan(halfFovRad);
        float frustumWidth = frustumHeight * cam.aspect;

        float minX = bounds.min.x + frustumWidth * 0.5f;
        float maxX = bounds.max.x - frustumWidth * 0.5f;
        float minY = bounds.min.y + frustumHeight * 0.5f;
        float maxY = bounds.max.y - frustumHeight * 0.5f;

        if (minX > maxX)
        {
            minX = maxX = bounds.center.x;
        }
        if (minY > maxY)
        {
            minY = maxY = bounds.center.y;
        }

        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);

        Vector3 clampedTarget = new Vector3(clampedX, clampedY, targetPos.z);

        Vector3 smoothed = Vector3.Lerp(transform.position, clampedTarget, smoothSpeed);
        transform.position = smoothed;

        transform.rotation = ComputeCenterLookRotation(smoothed, subject);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Mathf.Clamp01(fovLerpSpeed * Time.deltaTime));
    }

    private void OnRoomChanged(RoomComponent previous, RoomComponent current)
    {
        if (current == null)
        {
            offset = defaultOffset;
            return;
        }
        // Switch offset based on tag.
        if (current.CompareTag(hallwayTag))
        {
            offset = hallwayOffset;
        }
        else
        {
            offset = defaultOffset;
        }
    }

    private void ResolvePlayerRoomAtPosition()
    {
        if (playerMovement == null) return;
        Vector3 playerPos = playerMovement.transform.position;
        // Small overlap sphere to find room _colliders quickly.
        Collider[] hits = Physics.OverlapSphere(playerPos, 0.05f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var room = hits[i].GetComponent<RoomComponent>();
            if (room != null && room.Bounds.Contains(playerPos))
            {
                currentRoom = room;
                return;
            }
        }
        // keep previous if none found
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!blackoutOutsideRoom || blackoutMaterial == null || currentRoom == null)
        {
            Graphics.Blit(src, dst);
            return;
        }
        Graphics.Blit(src, dst, blackoutMaterial);
    }

    private Quaternion ComputeCenterLookRotation(Vector3 cameraPosition, Transform lookSubject)
    {
        Vector3 toTarget = lookSubject != null ? (lookSubject.position - cameraPosition) : Vector3.forward;
        if (toTarget.sqrMagnitude < 0.0001f)
            return transform.rotation;

        Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        Vector3 desiredEuler = desired.eulerAngles;
        float signedYaw = Mathf.DeltaAngle(0f, desiredEuler.y);
        float clampedYaw = Mathf.Clamp(signedYaw, -40f, 40f);
        Vector3 targetEuler = new Vector3(desiredEuler.x, clampedYaw, 0f);
        Quaternion target = Quaternion.Euler(targetEuler);

        float t = Mathf.Clamp01(focusSlerpSpeed * Time.deltaTime);
        return Quaternion.Slerp(transform.rotation, target, t);
    }

    public Vector3 GetOffset()
    {
        return offset;
    }

    // PUBLIC ZOOM API -------------------------------------------------------
    public void ZoomIn()
    {
        if (cam == null) return;
        targetFov = Mathf.Max(minFov, targetFov - zoomStep);
    }

    public void ZoomOut()
    {
        if (cam == null) return;
        targetFov = Mathf.Min(maxFov, targetFov + zoomStep);
    }

    // PUBLIC FOCUS OVERRIDE API --------------------------------------------
    public void FocusOnEnemy(Transform enemy, float duration = 0f)
    {
        if (enemy == null) return;
        lastEnemyFocus = enemy;
        FocusOnTarget(enemy, duration);
    }

    // Overload without providing Transform; auto-acquire target.
    public void FocusOnEnemy(float duration = 0f)
    {
        var enemy = AcquireEnemyTarget();
        if (enemy != null)
            FocusOnEnemy(enemy, duration);
    }

    public void FocusOnTarget(Transform target, float duration = 0f)
    {
        focusTarget = target;
        if (focusOverrideRoutine != null)
            StopCoroutine(focusOverrideRoutine);
        if (duration > 0f)
            focusOverrideRoutine = StartCoroutine(FocusOverrideTimer(duration));
    }

    public void RevertFocusToPlayer()
    {
        if (focusOverrideRoutine != null)
        {
            StopCoroutine(focusOverrideRoutine);
            focusOverrideRoutine = null;
        }
        focusTarget = player;
    }

    public bool IsFocusingOnPlayer()
    {
        return focusTarget == player || focusTarget == null;
    }

    private IEnumerator FocusOverrideTimer(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (focusTarget == null) break; // abort if target lost
            elapsed += Time.deltaTime;
            yield return null;
        }
        RevertFocusToPlayer();
    }

    private Transform AcquireEnemyTarget()
    {
        if (lastEnemyFocus != null) return lastEnemyFocus;
        if (player == null) return null;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float bestDist = float.MaxValue;
        Transform best = null;
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            float d = (e.transform.position - player.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e.transform;
            }
        }
        lastEnemyFocus = best;
        return best;
    }

    public void SetBlackoutEnabled(bool enabled)
    {
        blackoutOutsideRoom = enabled;
        if (enabled && cam != null)
            cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    public void SetEnemyFocusOffset(Vector3 newOffset)
    {
        enemyFocusOffset = newOffset;
    }
    public Vector3 GetEnemyFocusOffset() => enemyFocusOffset;
}