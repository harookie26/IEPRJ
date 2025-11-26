using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PBManual : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Room Constraint")]
    [Tooltip("If enabled, PB movement is clamped to the bounds of the room the PLAYER is currently in.")]
    [SerializeField] private bool restrictToPlayerRoomBounds = true;

    [Tooltip("Also clamp Y to keep the PB fully inside the room volume (including ceiling/floor).")]
    [SerializeField] private bool clampYInsideRoomVolume = false;

    [Tooltip("Cached reference to the room the PLAYER is currently inside.")]
    [SerializeField] private RoomComponent playerCurrentRoom;

    [Tooltip("PlayerMovement used to resolve the player's current room. Auto-detected at runtime if not assigned.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Boundary State (read-only)")]
    [Tooltip("True when the paintbrush push attempt is clamped along at least two horizontal axes (corner).")]
    public bool IsAtCorner { get; private set; }
    [Tooltip("True when the paintbrush push attempt is clamped along any axis.")]
    public bool IsAgainstBoundary { get; private set; }

    private Rigidbody rb;

    // --- Glue player state ---
    private bool playerWasDisabledByPB = false;
    private bool isGluingPlayer = false;
    private Vector3 gluedPlayerPosition;
    private Quaternion gluedPlayerRotation;
    private Rigidbody playerRb;
    private RigidbodyConstraints previousPlayerConstraints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        // When PBManual becomes active (manual mode), disable player movement and glue player in place.
        if (playerMovement != null)
        {
            playerWasDisabledByPB = true;
            playerMovement.SetCanMove(false);

            // cache rigidbody and constraints
            playerRb = playerMovement.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                previousPlayerConstraints = playerRb.constraints;
                playerRb.constraints = RigidbodyConstraints.FreezeAll;
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            gluedPlayerPosition = playerMovement.transform.position;
            gluedPlayerRotation = playerMovement.transform.rotation;
            isGluingPlayer = true;
        }
    }

    private void OnDisable()
    {
        // Restore player movement and physics state when manual mode ends
        if (playerWasDisabledByPB && playerMovement != null)
        {
            playerMovement.SetCanMove(true);
            playerWasDisabledByPB = false;
        }

        if (playerRb != null)
        {
            playerRb.constraints = previousPlayerConstraints;
            playerRb = null;
        }

        isGluingPlayer = false;
    }

    private void Start()
    {
        ResolvePlayerRoomAtPosition();
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // Keep player glued while PBManual is active
        if (isGluingPlayer && playerMovement != null)
        {
            if (playerRb != null)
            {
                // Move the rigidbody to the cached position/rotation each physics step
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
                playerRb.MovePosition(gluedPlayerPosition);
                playerRb.MoveRotation(gluedPlayerRotation);
            }
            else
            {
                playerMovement.transform.position = gluedPlayerPosition;
                playerMovement.transform.rotation = gluedPlayerRotation;
            }
        }

        rb.angularVelocity = Vector3.zero;

        float moveInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
        float strafeInput = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        float verticalInput = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude < 0.0001f) camRight = Vector3.right;
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput;
        Vector3 strafeDir = camRight * strafeInput;

        Vector3 desiredVelocity = (moveDir + strafeDir) * moveSpeed;
        desiredVelocity.y = verticalInput * moveSpeed;

        ResolvePlayerRoomAtPosition();

        float dt = Time.fixedDeltaTime;
        Vector3 unclampedTargetPos = rb.position + desiredVelocity * dt;
        Vector3 targetPos = unclampedTargetPos;

        bool shouldClampToRoom = restrictToPlayerRoomBounds && playerCurrentRoom != null;
        if (shouldClampToRoom)
        {
            targetPos = ClampPositionToRoom(unclampedTargetPos, playerCurrentRoom.Bounds, clampYInsideRoomVolume);

            bool xClamped = !Mathf.Approximately(targetPos.x, unclampedTargetPos.x);
            bool zClamped = !Mathf.Approximately(targetPos.z, unclampedTargetPos.z);
            bool yClamped = clampYInsideRoomVolume && !Mathf.Approximately(targetPos.y, unclampedTargetPos.y);

            IsAgainstBoundary = xClamped || zClamped || yClamped;
            // Consider “corner” when horizontally clamped on 2 axes (X and Z) in the same step.
            IsAtCorner = (xClamped ? 1 : 0) + (zClamped ? 1 : 0) >= 2;
        }
        else
        {
            IsAgainstBoundary = false;
            IsAtCorner = false;
        }

        Vector3 correctedVelocity = (targetPos - rb.position) / Mathf.Max(dt, 0.0001f);
        rb.linearVelocity = correctedVelocity;

        Vector3 lookDir = cameraTransform.forward;
        if (lookDir.sqrMagnitude < 0.0001f)
            lookDir = Vector3.forward;

        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
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
}