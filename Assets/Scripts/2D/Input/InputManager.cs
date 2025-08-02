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

        // Initialize input actions if using the new Input System
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

        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, () => inputActions?.Disable());
        EventBroadcaster.Instance.AddObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, () => inputActions?.Enable());
    }

    private void OnDisable()
    {
        inputActions?.Disable();

        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED, () => inputActions?.Disable());
        EventBroadcaster.Instance.RemoveActionAtObserver(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED, () => inputActions?.Enable());
    }


    private void Update()
    {
        interactPressed = Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
        channelPressed = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        shoutPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    // Polling API for other scripts
    public Vector2 GetMoveInput() => moveInput;
    public bool IsSprinting() => sprintHeld;
    public bool WasInteractPressed() => interactPressed;
    public bool WasChannelPressed() => channelPressed;
    public bool WasShoutPressed() => shoutPressed;
    public bool WasAnyKeyExceptChannelPressed()
    {
        if (Keyboard.current == null) return false;

        return Keyboard.current.anyKey.wasPressedThisFrame && !Keyboard.current.qKey.wasPressedThisFrame;
    }

    /// <summary>
    /// Waits until any key or mouse button is pressed, then calls onComplete.
    /// </summary>
    public System.Collections.IEnumerator WaitForInputCoroutine(Action onComplete)
    {
        yield return new WaitForEndOfFrame();

        while (
            (Keyboard.current == null || !Keyboard.current.anyKey.wasPressedThisFrame) &&
            (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.rightButton.wasPressedThisFrame)
        )
        {
            yield return null;
        }

        onComplete?.Invoke();
    }
}
