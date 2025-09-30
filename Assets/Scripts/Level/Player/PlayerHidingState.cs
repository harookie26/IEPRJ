using UnityEngine;
using Game.States;
using UnityEngine.InputSystem;

public class PlayerHidingState : IPlayerState
{
    readonly PlayerStateMachine _owner;
    readonly float _maxHideTime;
    float _timer;

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
    float _wallMoveSpeed = 3f; // You can tweak this

    bool _ignoreInteractThisFrame = true;

    public PlayerHidingState(PlayerStateMachine owner, Vector3? wallPoint = null, Vector3? wallNormal = null, float maxHideTime = 10f)
    {
        _owner = owner;
        _maxHideTime = maxHideTime;
        _timer = 0f;
        _wallPoint = wallPoint;
        _wallNormal = wallNormal;
    }

    public void Enter()
    {
        _timer = 0f;
        _ignoreInteractThisFrame = true;
        Debug.Log("PlayerHidingState: Enter");

        // Cache PlayerMovement and disable its normal movement
        _playerMovement = _owner.GetComponent<PlayerMovement>();
        if (_playerMovement != null)
        {
            _wasMovementEnabled = _playerMovement.enabled;
            _playerMovement.enabled = false;
        }

        if (_wallPoint.HasValue && _wallNormal.HasValue)
        {
            Debug.Log($"PlayerHidingState: Hiding against wall at {_wallPoint.Value} with normal {_wallNormal.Value}");

            // Start interpolation
            var player = _owner.gameObject;
            _startPosition = player.transform.position;
            _startRotation = player.transform.rotation;

            // Offset the player slightly away from the wall to avoid clipping
            float offset = 0.5f; // adjust as needed for your player size
            _targetPosition = _wallPoint.Value + _wallNormal.Value.normalized * offset;

            // Face outward from the wall (opposite the wall normal)
            _targetRotation = Quaternion.LookRotation(-_wallNormal.Value.normalized, Vector3.up);

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
        // Restore PlayerMovement
        if (_playerMovement != null)
            _playerMovement.enabled = _wasMovementEnabled;

        _owner.NotifyHidingExited();
    }

    public void HandleInput()
    {
        if (_ignoreInteractThisFrame)
        {
            _ignoreInteractThisFrame = false;
            return;
        }

        // Keyboard fallback
        if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Space))
        {
            _owner.SetToDefaultState();
            return;
        }

        // InputManager interact key (E by default, or whatever is mapped)
        if (InputManager.Instance != null && InputManager.Instance.WasInteractPressed())
        {
            _owner.SetToDefaultState();
            return;
        }
    }
    public void Tick()
    {
        // Interpolate to wall if needed
        if (_isSnapping)
        {
            _snapElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_snapElapsed / _snapDuration);

            var player = _owner.gameObject;
            player.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
            player.transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            if (t >= 1f)
            {
                _isSnapping = false;
            }
        }
        else if (_wallNormal.HasValue)
        {
            // Only allow A/D (left/right) movement along the wall tangent
            float input = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input = 1f;

            if (Mathf.Abs(input) > 0.01f)
            {
                // Calculate wall tangent (cross wall normal with up)
                Vector3 wallNormal = _wallNormal.Value.normalized;
                Vector3 wallTangent = Vector3.Cross(Vector3.up, wallNormal).normalized;

                // Move player along the wall tangent
                var player = _owner.gameObject;
                player.transform.position += wallTangent * input * _wallMoveSpeed * Time.deltaTime;
            }
        }

        // Auto-exit after max hide time
        _timer += Time.deltaTime;
        if (_timer >= _maxHideTime)
        {
            _owner.SetToDefaultState();
        }
    }
}