using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;

    [SerializeField] float moveSpeed = 5f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Movement"];
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Get the camera's forward and right vectors, ignoring the y component
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement direction relative to camera
        Vector3 move = (camRight * input.x + camForward * input.y) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}
