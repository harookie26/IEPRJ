using UnityEngine;
using Game.States;
using UnityEngine.InputSystem;

public class PlayerHidingState : IPlayerState
{
    readonly PlayerStateMachine _owner;

    // Wall snap info
    readonly Vector3? _wallPoint;
    readonly Vector3? _wallNormal;

    // Interpolation
    bool _isSnapping;
    float _snapDuration = 0.25f; // seconds
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

    bool _ignoreInteractThisFrame = true;

    // Nudge feedback
    float _nudgeDistance = 0.5f;
    float _nudgeDuration = 0.15f;
    float _nudgeElapsed = 0f;
    bool _isNudging = false;
    Vector3 _nudgeStart;
    Vector3 _nudgeEnd;

    // Removed timed auto-exit: player can now hide indefinitely.
    public PlayerHidingState(PlayerStateMachine owner, Vector3? wallPoint = null, Vector3? wallNormal = null)
    {
        _owner = owner;
        _wallPoint = wallPoint;
        _wallNormal = wallNormal;
    }

    public void Enter()
    {
        _ignoreInteractThisFrame = true;
        Debug.Log("PlayerHidingState: Enter");

        _playerMovement = _owner.GetComponent<PlayerMovement>();
        if (_playerMovement != null)
        {
            _wasMovementEnabled = _playerMovement.enabled;
            _playerMovement.enabled = false;
        }

        if (_wallPoint.HasValue && _wallNormal.HasValue)
        {
            Debug.Log($"PlayerHidingState: Hiding against wall at {_wallPoint.Value} with normal {_wallNormal.Value}");

            var player = _owner.gameObject;
            _startPosition = player.transform.position;
            _startRotation = player.transform.rotation;

            float offset = 0.5f;
            Vector3 outward = _wallNormal.Value.normalized; // normal already points outward
            _targetPosition = _wallPoint.Value + _wallNormal.Value.normalized * offset;
            _targetRotation = Quaternion.LookRotation(outward, Vector3.up);

            _isSnapping = true;
            _snapElapsed = 0f;
        }
        else
        {
            Debug.Log("PlayerHidingState: Hiding (no wall info provided)");
            _isSnapping = false;
        }

        _owner.NotifyHidingEntered();
    }

    public void Exit()
    {
        Debug.Log("PlayerHidingState: Exit");

        if (_wallNormal.HasValue)
        {
            var player = _owner.gameObject;
            _nudgeStart = player.transform.position;
            _nudgeEnd = _nudgeStart + _wallNormal.Value.normalized * _nudgeDistance;
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

        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Space))
        {
            _owner.SetToDefaultState();
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
        var player = _owner.gameObject;

        if (_isSnapping)
        {
            _snapElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_snapElapsed / _snapDuration);
            player.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
            player.transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            if (t >= 1f)
                _isSnapping = false;
        }
        else if (_wallNormal.HasValue)
        {
            float input = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input = 1f;

            Vector3 wallNormal = _wallNormal.Value.normalized;
            Vector3 outward = wallNormal;
            if (outward.sqrMagnitude < 0.0001f) outward = player.transform.forward;

            Vector3 wallTangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            if (wallTangent.sqrMagnitude < 0.0001f)
                wallTangent = Vector3.Cross(outward, Vector3.up).normalized;

            if (Mathf.Abs(input) > 0.01f)
                wallTangent = -wallTangent;
                player.transform.position += wallTangent * input * _wallMoveSpeed * Time.deltaTime;

            if (_rotateTowardMovementAlongWall)
            {
                Vector3 desiredForward = Mathf.Abs(input) > 0.01f ? (wallTangent * input) : outward;
                if (desiredForward.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.deltaTime * _rotationLerpSpeed);
                }
            }
            else
            {
                Quaternion targetRot = Quaternion.LookRotation(outward, Vector3.up);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.deltaTime * _rotationLerpSpeed);
            }
        }

        // Removed timed auto-exit: player stays hidden until player input exits.
    }
}