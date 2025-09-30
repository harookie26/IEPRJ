using UnityEngine;
using System;
using Game.States;

public class PlayerStateMachine : MonoBehaviour
{
    IPlayerState _currentState;

    // Events other systems can subscribe to (movement, animator, colliders...)
    public event Action HidingEntered;
    public event Action HidingExited;

    // Add these fields:
    private float hideCooldown = 0.2f; // seconds
    private float hideCooldownTimer = 0f;

    void Start()
    {
        // start in default state
        SetToDefaultState();
    }

    void Update()
    {
        if (hideCooldownTimer > 0f)
            hideCooldownTimer -= Time.deltaTime;

        if (_currentState != null)
        {
            _currentState.HandleInput();
            _currentState.Tick();
        }
    }

    public void SetState(IPlayerState newState)
    {
        if (_currentState != null)
            _currentState.Exit();

        _currentState = newState;

        if (_currentState != null)
            _currentState.Enter();
    }

    // Public helper to request hiding from other scripts (e.g. input/AI)
    // Accept optional wall information so an interactable can provide a snap point / normal.
    public void RequestHide(Vector3? wallPoint = null, Vector3? wallNormal = null)
    {
        // Prevent hiding if cooldown is active
        if (hideCooldownTimer > 0f)
            return;

        SetState(new PlayerHidingState(this, wallPoint, wallNormal));
    }

    // Called by states or external systems to go back to the default state
    public void SetToDefaultState()
    {
        SetState(new DefaultState(this));
        hideCooldownTimer = hideCooldown; // Start cooldown when exiting hiding
    }

    // These methods allow other types (states) to request the event be raised.
    // Events can only be invoked from within the declaring type, so expose raisers here.
    public void NotifyHidingEntered()
    {
        HidingEntered?.Invoke();
    }

    public void NotifyHidingExited()
    {
        HidingExited?.Invoke();
    }

    // Internal default/idle state
    class DefaultState : IPlayerState
    {
        readonly PlayerStateMachine _owner;

        public DefaultState(PlayerStateMachine owner)
        {
            _owner = owner;
        }

        public void Enter()
        {
            Debug.Log("PlayerStateMachine: Enter DefaultState");
        }

        public void Exit()
        {
            Debug.Log("PlayerStateMachine: Exit DefaultState");
        }

        public void HandleInput()
        {
            // Example: pressing H requests hiding (can be changed to any input)
            if (Input.GetKeyDown(KeyCode.H))
            {
                _owner.RequestHide();
            }
        }

        public void Tick()
        {
            // default per-frame logic
        }
    }
}