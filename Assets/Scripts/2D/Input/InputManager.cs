using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private Vector2 moveInput;
    private bool sprintHeld;

    private bool interactPressed;
    private bool channelPressed;
    private bool shoutPressed;

    private InputSystem2D inputActions;

    private bool onlyAllowLMBOrEnter = false;

    // New: Block input until all keys/buttons are released after re-enabling
    private bool blockInputUntilRelease = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        inputActions = new InputSystem2D();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Sprint.performed += ctx =>
        {
            sprintHeld = true;
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STARTED_SPRINT);
        };
        inputActions.Player.Sprint.canceled += ctx =>
        {
            sprintHeld = false;
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED_SPRINT);
        };
    }

    private void OnEnable()
    {
        inputActions?.Enable();

        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERCONTROLS_DISABLED, OnPlayerControlsDisabled);
        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERCONTROLS_ENABLED, OnPlayerControlsEnabled);
        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, () => inputActions?.Disable());
        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, () => inputActions?.Enable());
    }

    private void OnDisable()
    {
        inputActions?.Disable();

        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERCONTROLS_DISABLED, OnPlayerControlsDisabled);
        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERCONTROLS_ENABLED, OnPlayerControlsEnabled);
        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, () => inputActions?.Disable());
        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, () => inputActions?.Enable());
    }

    private void OnPlayerControlsDisabled()
    {
        inputActions?.Disable();
        onlyAllowLMBOrEnter = true;
    }

    private void OnPlayerControlsEnabled()
    {
        inputActions?.Enable();
        onlyAllowLMBOrEnter = false;
        blockInputUntilRelease = true; // Block input until all keys/buttons are released
    }

    private void Update()
    {
        // Block input until all keys/buttons are released after re-enabling
        if (blockInputUntilRelease)
        {
            if (
                (Keyboard.current == null || !Keyboard.current.anyKey.isPressed) &&
                (Mouse.current == null || (!Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed))
            )
            {
                blockInputUntilRelease = false; // All released, resume input
            }
            else
            {
                // While blocked, clear all input states
                interactPressed = false;
                channelPressed = false;
                shoutPressed = false;
                moveInput = Vector2.zero;
                sprintHeld = false;
                return;
            }
        }

        if (onlyAllowLMBOrEnter)
        {
            interactPressed = false;
            channelPressed = false;
            shoutPressed = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
        }
        else
        {
            interactPressed = Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
            channelPressed = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
            shoutPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }
    }

    // Polling API for other scripts
    public Vector2 GetMoveInput() => (onlyAllowLMBOrEnter || blockInputUntilRelease) ? Vector2.zero : moveInput;
    public bool IsSprinting() => !onlyAllowLMBOrEnter && !blockInputUntilRelease && sprintHeld;
    public bool WasInteractPressed() => !blockInputUntilRelease && interactPressed;
    public bool WasChannelPressed() => !onlyAllowLMBOrEnter && !blockInputUntilRelease && channelPressed;
    public bool WasShoutPressed() => !onlyAllowLMBOrEnter && !blockInputUntilRelease && shoutPressed;
    public bool WasAnyKeyExceptChannelPressed()
    {
        if (Keyboard.current == null || blockInputUntilRelease) return false;

        if (onlyAllowLMBOrEnter)
        {
            return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                   Keyboard.current.enterKey.wasPressedThisFrame;
        }

        return Keyboard.current.anyKey.wasPressedThisFrame && !Keyboard.current.qKey.wasPressedThisFrame;
    }

    public System.Collections.IEnumerator WaitForInputCoroutine(Action onComplete)
    {
        // Wait until all keys and mouse buttons are released
        while (
            (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
            (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
        )
        {
            yield return null;
        }

        // Now wait for a new press
        bool pressed = false;
        while (!pressed)
        {
            if (onlyAllowLMBOrEnter)
            {
                if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                    (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame))
                {
                    pressed = true;
                }
            }
            else
            {
                if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                    (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)))
                {
                    pressed = true;
                }
            }
            yield return null;
        }

        onComplete?.Invoke();
    }
}
