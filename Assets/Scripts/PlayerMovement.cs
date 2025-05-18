using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;

    public float turnSpeed = 180f;
    [SerializeField] float moveSpeed = 5f;

    public bool isRotating = false;
    private float targetYRotation;
    private float rotationDirection = 0f;

    private Transform cameraTransform;
    private float prevHorizontalInput = 0f; // Track previous frame's horizontal input

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Movement");
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        float cameraY = cameraTransform.eulerAngles.y;
        float playerY = transform.eulerAngles.y;

        // Only trigger rotation on the initial press of A or D (rising edge)
        if (!isRotating && Mathf.Abs(input.x) > 0.1f && Mathf.Abs(prevHorizontalInput) < 0.1f)
        {
            isRotating = true;
            rotationDirection = Mathf.Sign(input.x);
            float nextTarget = Mathf.Round(playerY / 90f) * 90f + 90f * rotationDirection;

            // Clamp the target rotation within ±90° of the camera's Y axis
            float deltaToCamera = Mathf.DeltaAngle(cameraY, nextTarget);
            deltaToCamera = Mathf.Clamp(deltaToCamera, -90f, 90f);
            targetYRotation = cameraY + deltaToCamera;

            // Nudge forward slightly when starting to rotate
            Vector3 forward = transform.forward;
            transform.position += forward * moveSpeed * Time.deltaTime * 0.5f; // 20% of normal move

        }

        if (isRotating)
        {
            // Rotate towards the target angle
            float newY = Mathf.MoveTowardsAngle(playerY, targetYRotation, turnSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0, newY, 0);

            // Check if rotation is complete
            if (Mathf.Approximately(Mathf.DeltaAngle(playerY, targetYRotation), 0f))
            {
                isRotating = false;
            }
        }
        else
        {
            // If A or D is still held after rotation, move forward in the new direction
            if (Mathf.Abs(input.x) > 0.1f)
            {
                Vector3 forward = transform.forward;
                transform.position += forward * moveSpeed * Time.deltaTime;
            }
            // Or, if W is pressed, move forward as usual
            else if (input.y > 0.1f)
            {
                Vector3 forward = transform.forward;
                transform.position += forward * moveSpeed * Time.deltaTime;
            }
        }

        // Store current horizontal input for next frame
        prevHorizontalInput = input.x;
    }
}
