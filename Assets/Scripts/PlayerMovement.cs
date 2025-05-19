using UnityEditor.Rendering;
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
    private float prevHorizontalInput = 0f;
    private float prevVerticalInput = 0f; // Track previous frame's vertical input

    [SerializeField] float snapThreshold = 45f; // Degrees

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Movement");
        cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        float playerY = transform.eulerAngles.y;
        float cameraY = cameraTransform.eulerAngles.y;

        // Prioritize horizontal input (A/D) over vertical input (W)
        if (Mathf.Abs(input.x) > 0.1f)
        {
            // Only trigger 90° snap rotation on the initial press of A or D (rising edge)
            if (!isRotating && Mathf.Abs(prevHorizontalInput) < 0.1f)
            {
                isRotating = true;
                rotationDirection = Mathf.Sign(input.x);
                targetYRotation = Mathf.Round(playerY / 90f) * 90f + 90f * rotationDirection;
            }

            if (isRotating)
            {
                // Nudge forward slightly when starting to rotate
                Vector3 forward = transform.forward;
                transform.position += forward * moveSpeed * Time.deltaTime * 0.5f;

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
                // Move forward in the new direction while holding A or D
                Vector3 forward = transform.forward;
                transform.position += forward * moveSpeed * Time.deltaTime;
            }

            // Do not process W if A or D is held
        }
        else if (input.y > 0.1f)
        {
            // Calculate angle difference
            float angleDiff = Mathf.DeltaAngle(playerY, cameraY);

            // Snap if the gap is large
            if (Mathf.Abs(angleDiff) > snapThreshold)
            {
                transform.eulerAngles = new Vector3(0, cameraY, 0);
            }
            else
            {
                // Smoothly rotate towards camera's Y
                float newY = Mathf.MoveTowardsAngle(playerY, cameraY, turnSpeed * Time.deltaTime);
                transform.eulerAngles = new Vector3(0, newY, 0);
            }

            // Move forward in the new direction
            Vector3 forward = transform.forward;
            transform.position += forward * moveSpeed * Time.deltaTime;

            isRotating = false;
        }

        // Store current input for next frame
        prevHorizontalInput = input.x;
        prevVerticalInput = input.y;
    }

}
