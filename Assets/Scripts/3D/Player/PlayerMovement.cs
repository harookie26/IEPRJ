using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;

    [SerializeField] float moveSpeed = 5f;

    private Vector2 previousInput = Vector2.zero;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
    }

    private void Update()
    {
        // Prevent movement if companion is in manual mode
        if (PBController.IsCompanionManualModeActive)
        {
            previousInput = Vector2.zero;
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();

        // Get the camera's forward and right vectors, ignoring the y component
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        if (input != Vector2.zero)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_MOVED);
        }
        else if (previousInput != Vector2.zero)
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_STOPPED);
        }

        previousInput = input;

        // Calculate movement direction relative to camera
        Vector3 move = (camRight * input.x + camForward * input.y) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}