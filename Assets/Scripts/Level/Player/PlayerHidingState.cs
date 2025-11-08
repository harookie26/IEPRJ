using UnityEngine;
using Game.States;
using UnityEngine.InputSystem;

public class PlayerHidingState : IPlayerState
{
    readonly PlayerStateMachine _owner;

    // Wall snap info (public API surface extended)
    readonly Vector3? _wallPoint;
    readonly Vector3? _wallNormal;
    readonly Vector3? _providedWallTangent; // NEW: optional externally supplied tangent (e.g. from WallHideAnchor)

    // Derived wall basis
    Vector3 _basisNormal;   // normalized
    Vector3 _basisTangent;  // lateral movement axis (A/D)
    Vector3 _basisBinormal; // unused (could be up-on-wall if needed)

    // Keep plane anchor & offset for stable re-projection
    Vector3 _wallPlanePoint;
    float _hideOffset = 0.5f;

    // Interpolation
    bool _isSnapping;
    float _snapDuration = 0.25f;
    float _snapElapsed = 0f;
    Vector3 _startPosition;
    Quaternion _startRotation;
    Vector3 _targetPosition;
    Quaternion _targetRotation;

    // Wall sliding
    PlayerMovement _playerMovement;
    bool _wasMovementEnabled;
    float _wallMoveSpeed = 3f;

    // Rotation while hiding
    float _rotationLerpSpeed = 10f;
    bool _rotateTowardMovementAlongWall = true;
    bool _maintainUpright = true; // NEW: ensure player stays vertically upright

    bool _ignoreInteractThisFrame = true;

    // Nudge feedback
    float _nudgeDistance = 0.5f;
    float _nudgeDuration = 0.15f;
    float _nudgeElapsed = 0f;
    bool _isNudging = false;
    Vector3 _nudgeStart;
    Vector3 _nudgeEnd;

    public PlayerHidingState(
        PlayerStateMachine owner,
        Vector3? wallPoint = null,
        Vector3? wallNormal = null,
        Vector3? wallTangent = null) // NEW parameter
    {
        _owner = owner;
        _wallPoint = wallPoint;
        _wallNormal = wallNormal;
        _providedWallTangent = wallTangent;
    }

    public void Enter()
    {
        _ignoreInteractThisFrame = true;
        _playerMovement = _owner.GetComponent<PlayerMovement>();
        if (_playerMovement != null)
        {
            _wasMovementEnabled = _playerMovement.enabled;
            _playerMovement.enabled = false;
        }

        if (_wallPoint.HasValue && _wallNormal.HasValue)
        {
            // Reject near-up normals (optional)
            if (Mathf.Abs(_wallNormal.Value.y) > 0.95f)
            {
                _owner.SetToDefaultState();
                return;
            }

            _basisNormal = _wallNormal.Value.normalized;
            _wallPlanePoint = _wallPoint.Value;

            // 1. Determine tangent:
            if (_providedWallTangent.HasValue)
            {
                // Use supplied tangent but ensure orthogonality to normal.
                Vector3 t = _providedWallTangent.Value;
                // Remove any component along normal to avoid drift.
                t -= Vector3.Dot(t, _basisNormal) * _basisNormal;
                if (t.sqrMagnitude < 1e-4f)
                {
                    // Fallback if badly aligned
                    t = Vector3.Cross(Vector3.up, _basisNormal);
                }
                _basisTangent = t.normalized;
            }
            else
            {
                // Derive from world up first.
                Vector3 tentative = Vector3.Cross(Vector3.up, _basisNormal);
                if (tentative.sqrMagnitude < 1e-4f)
                {
                    // Normal is (almost) vertical; fallback: use player's right or any horizontal axis
                    tentative = Vector3.Cross(_owner.transform.right, _basisNormal);
                }
                _basisTangent = tentative.normalized;
            }

            // 2. Binormal (not strictly needed now, but could help for advanced logic)
            _basisBinormal = Vector3.Cross(_basisNormal, _basisTangent).normalized;

            // 3. Outward for positioning & rotation
            Vector3 outwardForOffset = _basisNormal;

            // Keep player upright for visual rotation if requested
            Vector3 outwardForRotation = outwardForOffset;
            if (_maintainUpright)
            {
                outwardForRotation = Vector3.ProjectOnPlane(outwardForOffset, Vector3.up);
                if (outwardForRotation.sqrMagnitude < 1e-4f)
                    outwardForRotation = _owner.transform.forward; // fallback
                outwardForRotation.Normalize();
            }

            var player = _owner.gameObject;
            _startPosition = player.transform.position;
            _startRotation = player.transform.rotation;

            _targetPosition = _wallPlanePoint + outwardForOffset * _hideOffset;
            _targetRotation = Quaternion.LookRotation(outwardForRotation, Vector3.up);

            _isSnapping = true;
            _snapElapsed = 0f;
        }
        else
        {
            _isSnapping = false;
        }

        _owner.NotifyHidingEntered();
    }

    public void Exit()
    {
        if (_wallNormal.HasValue)
        {
            var player = _owner.gameObject;
            _nudgeStart = player.transform.position;
            _nudgeEnd = _nudgeStart + _basisNormal * _nudgeDistance;
            _nudgeElapsed = 0f;
            _isNudging = true;
            _owner.StartCoroutine(NudgeCoroutine(player));
        }

        if (_playerMovement != null)
            _playerMovement.enabled = _wasMovementEnabled;

        _owner.NotifyHidingExited();
    }

    private System.Collections.IEnumerator NudgeCoroutine(GameObject player)
    {
        while (_nudgeElapsed < _nudgeDuration)
        {
            _nudgeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_nudgeElapsed / _nudgeDuration);
            player.transform.position = Vector3.Lerp(_nudgeStart, _nudgeEnd, t);
            yield return null;
        }
        _isNudging = false;
    }

    public void HandleInput()
    {
        if (_ignoreInteractThisFrame)
        {
            _ignoreInteractThisFrame = false;
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.WasStealthPressed())
        {
            _owner.SetToDefaultState();
            return;
        }
    }

    public void Tick()
    {
        if (_isSnapping)
        {
            var playerSnap = _owner.gameObject;
            _snapElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_snapElapsed / _snapDuration);
            playerSnap.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
            playerSnap.transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);
            if (t >= 1f) _isSnapping = false;
            return;
        }

        if (!_wallNormal.HasValue) return;

        var player = _owner.gameObject;

        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input = 1f;

        if (Mathf.Abs(input) > 0.001f)
        {
            // Move along precomputed tangent
            Vector3 move = _basisTangent * input * _wallMoveSpeed * Time.deltaTime;
            Vector3 newPos = player.transform.position + move;

            // Re-project to maintain offset
            float currentDistance = Vector3.Dot(newPos - _wallPlanePoint, _basisNormal);
            float delta = currentDistance - _hideOffset;
            newPos -= _basisNormal * delta;

            player.transform.position = newPos;

            if (_rotateTowardMovementAlongWall)
            {
                Vector3 desiredForward = _basisTangent * input;
                if (_maintainUpright)
                {
                    desiredForward = Vector3.ProjectOnPlane(desiredForward, Vector3.up);
                    if (desiredForward.sqrMagnitude < 1e-4f)
                        desiredForward = player.transform.forward;
                }
                Quaternion targetRot = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.deltaTime * _rotationLerpSpeed);
            }
        }
        else if (!_rotateTowardMovementAlongWall)
        {
            // Face outward (keeping upright)
            Vector3 outward = _basisNormal;
            if (_maintainUpright)
            {
                outward = Vector3.ProjectOnPlane(outward, Vector3.up);
                if (outward.sqrMagnitude < 1e-4f) outward = player.transform.forward;
            }
            Quaternion targetRot = Quaternion.LookRotation(outward.normalized, Vector3.up);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.deltaTime * _rotationLerpSpeed);
        }
    }
}