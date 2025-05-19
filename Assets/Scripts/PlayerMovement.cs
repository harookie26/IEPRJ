using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;

    public float turnSpeed = 180f;
    [SerializeField] float moveSpeed = 5f;

    private float moveDelayTimer = 0f;
    [SerializeField] private float moveDelayDuration = 0.3f; // Adjust as needed
    [SerializeField] private float stepBackMultiplier = 0.3f; // Adjust as needed

    private Transform cameraTransform;

    // Flags for rotation angles
    public bool isRotating = false;
    public bool isRotatingWideToLeft = false;
    public bool isRotatingWideToRight = false;

    public bool isFacingCamera = false;

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

        // --- Facing camera logic ---
        Vector3 toCamera = (cameraTransform.position - transform.position).normalized;
        float facingAngle = Vector3.Angle(transform.forward, toCamera);
        isFacingCamera = facingAngle < 10f; // Adjust threshold as needed

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            float cameraY = cameraTransform.eulerAngles.y;
            Quaternion cameraRotation = Quaternion.Euler(0, cameraY, 0);
            Vector3 moveDirection = cameraRotation * inputDirection;
            moveDirection.Normalize();

            // Calculate angle between current forward and move direction
            float angle = Vector3.Angle(transform.forward, moveDirection);
            float signedAngle = Vector3.SignedAngle(transform.forward, moveDirection, Vector3.up);

            // Set flags
            bool wasRotatingWide = isRotatingWideToLeft || isRotatingWideToRight;
            isRotating = angle <= 120f;
            isRotatingWideToLeft = angle > 90f && angle <= 180f && signedAngle < 0f;
            isRotatingWideToRight = angle > 90f && angle <= 180f && signedAngle > 0f;
            bool isRotatingWide = isRotatingWideToLeft || isRotatingWideToRight;

            // If just started rotating wide, reset the delay timer
            if (isRotatingWide && !wasRotatingWide)
            {
                moveDelayTimer = moveDelayDuration;
            }

            // Rotate the player to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if (isRotatingWide)
            {
                // Step back a bit while rotating wide
                float stepBackSpeed = moveSpeed * stepBackMultiplier; // Adjust multiplier for desired effect
                transform.position -= transform.forward * stepBackSpeed * Time.deltaTime;
                // Keep the delay timer at its duration
                moveDelayTimer = moveDelayDuration;
            }
            else
            {
                // Only move forward if delay timer has expired
                if (moveDelayTimer > 0f)
                {
                    moveDelayTimer -= Time.deltaTime;
                }
                else
                {
                    transform.position += transform.forward * moveSpeed * Time.deltaTime;
                }
            }

        }
        else
        {
            isRotating = false;
            isRotatingWideToLeft = false;
            isRotatingWideToRight = false;
        }
    }


}
