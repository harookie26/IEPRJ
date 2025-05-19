using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;
    AnimationStateController animationStateController;

    public float turnSpeed = 180f;
    [SerializeField] float moveSpeed = 5f;

    public bool isRotating = false;
    private float targetYRotation;
    private float rotationDirection = 0f;

    private Transform cameraTransform;
    private float prevHorizontalInput = 0f;
    private float prevVerticalInput = 0f; // Track previous frame's vertical input

    public bool facingBack = false;

    [SerializeField] float snapThreshold = 45f; // Degrees

    // Flags for rotation angles
    public bool isRotating90OrLess = false;
    public bool isRotatingWide = false;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Movement");
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y);

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            float cameraY = cameraTransform.eulerAngles.y;
            Quaternion cameraRotation = Quaternion.Euler(0, cameraY, 0);
            Vector3 moveDirection = cameraRotation * inputDirection;
            moveDirection.Normalize();

            // Calculate angle between current forward and move direction
            float angle = Vector3.Angle(transform.forward, moveDirection);

            // Set flags
            isRotating90OrLess = angle <= 90f;
            isRotatingWide = angle > 90f && angle <= 180f;

            // Rotate the player to face the movement direction (in place)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            // Only move if rotation is nearly complete (angle is small)
            if (angle < 1f)
            {
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }
        }
        else
        {
            isRotating90OrLess = false;
            isRotatingWide = false;
        }
    }


}
