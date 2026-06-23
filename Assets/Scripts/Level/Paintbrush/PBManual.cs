using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[FoldableInspector]
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

    private Rigidbody _rb;

    private bool _playerWasDisabledByPB = false;
    private bool _isGluingPlayer = false;
    private Vector3 _gluedPlayerPosition;
    private Quaternion _gluedPlayerRotation;
    private Rigidbody _playerRb;
    private RigidbodyConstraints _previousPlayerConstraints;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (_rb != null)
            _rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        // When PBManual becomes active (manual mode), disable _player movement and glue _player in place.
        if (playerMovement != null)
        {
            _playerWasDisabledByPB = true;
            playerMovement.SetCanMove(false);

            // cache rigidbody and constraints
            _playerRb = playerMovement.GetComponent<Rigidbody>();
            if (_playerRb != null)
            {
                _previousPlayerConstraints = _playerRb.constraints;
                _playerRb.constraints = RigidbodyConstraints.FreezeAll;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            _gluedPlayerPosition = playerMovement.transform.position;
            _gluedPlayerRotation = playerMovement.transform.rotation;
            _isGluingPlayer = true;
        }
    }

    private void OnDisable()
    {
        ForceReleasePlayer();

        // Restore _player movement and physics state when manual mode ends
        if (_playerWasDisabledByPB && playerMovement != null)
        {
            playerMovement.SetCanMove(true);
            _playerWasDisabledByPB = false;
        }

        if (_playerRb != null)
        {
            _playerRb.constraints = _previousPlayerConstraints;
            _playerRb = null;
        }

        _isGluingPlayer = false;
    }

    private void Start()
    {
        ResolvePlayerRoomAtPosition();
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // Keep _player glued while PBManual is active
        if (_isGluingPlayer && playerMovement != null)
        {
            if (_playerRb != null)
            {
                // Move the rigidbody to the cached position/rotation each physics step
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
                _playerRb.MovePosition(_gluedPlayerPosition);
                _playerRb.MoveRotation(_gluedPlayerRotation);
            }
            else
            {
                playerMovement.transform.position = _gluedPlayerPosition;
                playerMovement.transform.rotation = _gluedPlayerRotation;
            }
        }

        _rb.angularVelocity = Vector3.zero;

        if (GameState.IsCutsceneActive)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

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
        Vector3 unclampedTargetPos = _rb.position + desiredVelocity * dt;
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

        Vector3 correctedVelocity = (targetPos - _rb.position) / Mathf.Max(dt, 0.0001f);
        _rb.linearVelocity = correctedVelocity;

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

    // Create a public method so PBController can forcefully trigger this cleanup
    public void ForceReleasePlayer()
    {
        if (_playerWasDisabledByPB && playerMovement != null)
        {
            playerMovement.SetCanMove(true);
            _playerWasDisabledByPB = false;
        }

        if (_playerRb != null)
        {
            // Hard reset to FreezeRotation so the player is guaranteed to be able to walk again
            _playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            _playerRb = null;
        }

        _isGluingPlayer = false;
    }
}
