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
    private bool corruptedRoomPressed;

    private InputSystem2D inputActions;

    private bool onlyAllowLMBOrEnter = false;

    private bool blockInputUntilRelease = false;

    private bool corruptedRoomMode = false;

    public event Action OnInteractPressed;

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
                moveInput = Vector2.zero;
                sprintHeld = false;
                return;
            }
        }

        if (onlyAllowLMBOrEnter)
        {
            interactPressed = false;
            corruptedRoomPressed = false;

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
            interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            // DETECT PRESS EVENT, do NOT post OFF every frame.
            corruptedRoomPressed = Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame;
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
