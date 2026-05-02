using UnityEngine;
using System;
using Game.States;

public class PlayerStateMachine : MonoBehaviour
{
    IPlayerState _currentState;

    public enum PlayerStateKind { Unknown, Default, Idle }
    public PlayerStateKind CurrentState { get; private set; } = PlayerStateKind.Unknown;

    public Type CurrentStateType => _currentState?.GetType();
    public string CurrentStateName => _currentState?.GetType().Name ?? "None";

    public bool IsState<T>() where T : IPlayerState => _currentState is T;

    public event Action IdleEntered;
    public event Action IdleExited;

    [Header("Idle")]
    [Tooltip("Seconds of no input before entering Idle state")]
    public float idleDelay = 3f;

    [Tooltip("Seconds between automatic restarts of the idle trail while player remains idle. Set to 0 to disable.")]
    public float idleTrailRestartInterval = 5f;

    [Header("VFX")]
    [Tooltip("Optional trail object to enable when player enters Idle state")]
    public TrailFollowDynamic idleTrail;

    private float idleTimer = 0f;

    void Start()
    {
        SetToDefaultState();
        idleTimer = idleDelay;
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
    }

    void Update()
    {
        // Detect any _player input / activity this frame
        bool inputDetected = DetectInput();

        if (inputDetected)
        {
            // reset idle timer when input occurs
            idleTimer = idleDelay;

            // if currently idle, leave immediately on input
            if (_currentState is IdleState)
            {
                SetToDefaultState();
            }
        }
        else
        {
            // count down to idle (but not if channeling)
            if (!(_currentState is IdleState))
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    SetState(new IdleState(this));
                }
            }
        }

        _currentState?.HandleInput();
        _currentState?.Tick();
    }

    public void SetToDefaultState()
    {
        SetState(new DefaultState(this));
        idleTimer = idleDelay;
    }

    public void NotifyIdleEntered() => IdleEntered?.Invoke();
    public void NotifyIdleExited() => IdleExited?.Invoke();
    public bool IsIdle => _currentState is IdleState;


    bool DetectInput()
    {
        if (Input.anyKey) return true;
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) return true;
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f) return true;
        if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f) return true;
        if (Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f) return true;
        if (Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f) return true;

        return false;
    }

    public void SetState(IPlayerState newState)
    {
        _currentState?.Exit();

        _currentState = newState;

        // update the public helper
        if (newState is IdleState) CurrentState = PlayerStateKind.Idle;
        else if (newState is DefaultState) CurrentState = PlayerStateKind.Default;
        else CurrentState = PlayerStateKind.Unknown;

        _currentState?.Enter();
    }

    class DefaultState : IPlayerState
    {
        readonly PlayerStateMachine _owner;
        public DefaultState(PlayerStateMachine owner) => _owner = owner;

        public void Enter() => Debug.Log("PlayerStateMachine: Enter DefaultState");
        public void Exit() => Debug.Log("PlayerStateMachine: Exit DefaultState");

        public void HandleInput()
        {
        }

        public void Tick() { }
    }

    class IdleState : IPlayerState
    {
        readonly PlayerStateMachine _owner;
        float _restartTimer;

        public IdleState(PlayerStateMachine owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("PlayerStateMachine: Enter IdleState");
            // Notify listeners that _player is idle
            _owner.NotifyIdleEntered();
            // Add any idle-specific setup here (e.g. play idle animation)

            // Activate trail VFX if assigned; try to find it if null
            if (_owner.idleTrail == null)
            {
                var found = GameObject.FindAnyObjectByType<TrailFollowDynamic>();
                if (found != null)
                {
                    _owner.idleTrail = found;
                }
            }

            if (_owner.idleTrail != null)
            {
                // ensure trail GameObject active then restart
                _owner.idleTrail.gameObject.SetActive(true);
                _owner.idleTrail.Restart();
                _restartTimer = 0f; // reset periodic restart timer
            }
            else
            {
                Debug.Log("PlayerStateMachine: idleTrail not assigned or found in scene.");
            }
        }

        public void Exit()
        {
            Debug.Log("PlayerStateMachine: Exit IdleState");
            _owner.NotifyIdleExited();

            // Disable trail VFX if assigned
            if (_owner.idleTrail != null)
            {
                _owner.idleTrail.Stop();
                _owner.idleTrail.gameObject.SetActive(false);
            }
        }

        public void HandleInput()
        {
            if (Input.anyKey ||
                Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f ||
                Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f ||
                Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f)
            {
                _owner.SetToDefaultState();
            }
        }

        public void Tick()
        {
            if (_owner.idleTrail != null && _owner.idleTrailRestartInterval > 0f)
            {
                _restartTimer += Time.deltaTime;
                if (_restartTimer >= _owner.idleTrailRestartInterval)
                {
                    _owner.idleTrail.gameObject.SetActive(true);
                    _owner.idleTrail.Restart();
                    _restartTimer = 0f;
                }
            }
        }
    }

}