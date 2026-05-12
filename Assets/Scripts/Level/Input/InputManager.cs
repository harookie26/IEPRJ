using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

[FoldableInspector]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private Vector2 moveInput;
    private bool sprintHeld;

    private bool interactPressed;
    private bool corruptedRoomPressed;
    private bool debugModePressed;

    private InputSystem_Actions inputActions;

    private bool onlyAllowLMBOrEnter = false;

    private bool blockInputUntilRelease = false;

    private bool corruptedRoomMode = false;

    public event Action OnInteractPressed;

    // Channeling events
    public event Action OnChannelStarted;
    public event Action OnChannelStopped;

    [Tooltip("Seconds the key must be held to count as a channel instead of a tap")]
    [SerializeField] private float channelHoldThreshold = 0.18f;

    // Internal channeling state
    private bool ePressPending = false;
    private float eHoldTimer = 0f;
    private bool channeling = false;

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

        inputActions = new InputSystem_Actions();
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
    }

    private void OnDisable()
    {
        inputActions?.Disable();
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
                blockInputUntilRelease = false;
            }
            else
            {
                // While blocked, clear all input states
                interactPressed = false;
                corruptedRoomPressed = false;
                debugModePressed = false;
                moveInput = Vector2.zero;
                sprintHeld = false;

                // reset channel state while blocked
                ePressPending = false;
                eHoldTimer = 0f;
                if (channeling)
                {
                    channeling = false;
                    OnChannelStopped?.Invoke();
                }

                return;
            }
        }

        // Reset per-frame interact & stealth unless we set them below
        interactPressed = false;

        if (onlyAllowLMBOrEnter)
        {
            // disable channeling while in this mode
            if (channeling)
            {
                channeling = false;
                OnChannelStopped?.Invoke();
            }
            ePressPending = false;
            eHoldTimer = 0f;

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
            // Tap vs Hold (channel) detection for E key
            bool eWasPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool eIsPressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            bool eWasReleased = Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame;

            if (eWasPressed)
            {
                ePressPending = true;
                eHoldTimer = 0f;
                Debug.Log("[InputManager] E was pressed (start tracking hold).");
            }

            if (ePressPending && eIsPressed)
            {
                eHoldTimer += Time.deltaTime;
                if (eHoldTimer >= channelHoldThreshold && !channeling)
                {
                    channeling = true;
                    OnChannelStarted?.Invoke();
                }
            }

            if (eWasReleased)
            {
                Debug.Log("[InputManager] E was released.");
                if (channeling)
                {
                    channeling = false;
                    OnChannelStopped?.Invoke();
                }
                else if (ePressPending)
                {
                    interactPressed = true;
                }

                ePressPending = false;
                eHoldTimer = 0f;
            }

            // DETECT PRESS EVENT, do NOT post OFF every frame.
            corruptedRoomPressed = Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame;

            // Debug mode toggle (F12)
            debugModePressed = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
        }

        if (corruptedRoomPressed)
        {
            corruptedRoomMode = !corruptedRoomMode;
            if (corruptedRoomMode)
                EventBroadcaster.Instance.PostEvent(LevelEvents.ON_CORRUPTED_ROOM_TRUE);
            else
                EventBroadcaster.Instance.PostEvent(LevelEvents.ON_CORRUPTED_ROOM_FALSE);
        }

        if (interactPressed && !blockInputUntilRelease)
        {
            OnInteractPressed?.Invoke();
        }

    }

    // Polling API for other scripts
    public Vector2 GetMoveInput() => (onlyAllowLMBOrEnter || blockInputUntilRelease) ? Vector2.zero : moveInput;
    public bool IsSprinting() => !onlyAllowLMBOrEnter && !blockInputUntilRelease && sprintHeld;
    public bool WasInteractPressed() => !blockInputUntilRelease && interactPressed;
    public bool IsChanneling() => !blockInputUntilRelease && !onlyAllowLMBOrEnter && channeling;
    public bool WasDebugModePressed() => !blockInputUntilRelease && debugModePressed;

    public bool WasAnyKeyExceptChannelPressed()
    {
        if (Keyboard.current == null || blockInputUntilRelease) return false;

        if (onlyAllowLMBOrEnter)
        {
            return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                   Keyboard.current.enterKey.wasPressedThisFrame;
        }

        // Exclude channel key (E) and stealth key (F) from this test
        return Keyboard.current.anyKey.wasPressedThisFrame
               && !Keyboard.current.qKey.wasPressedThisFrame
               && !Keyboard.current.eKey.wasPressedThisFrame
               && !Keyboard.current.fKey.wasPressedThisFrame;
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

    // --- NEW: UI mode helpers used by UI systems (debug panel, menus, etc.)
    /// <summary>
    /// Put InputManager into a UI-focused mode:
    /// - optionally restricts inputs to left-mouse/Enter (useful for click-only UI)
    /// - blocks processing until all buttons/keys are released (prevents accidental clicks)
    /// </summary>
    public void EnterUIMode(bool restrictToLmbOrEnter = true)
    {
        onlyAllowLMBOrEnter = restrictToLmbOrEnter;
        blockInputUntilRelease = true;
    }

    /// <summary>
    /// Exit UI-focused mode and block input until release to avoid immediate re-triggering
    /// of gameplay actions.
    /// </summary>
    public void ExitUIMode()
    {
        onlyAllowLMBOrEnter = false;
        blockInputUntilRelease = true;
    }
}