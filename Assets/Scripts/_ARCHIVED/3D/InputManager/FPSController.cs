using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField] private float moveSpeed = 3.0f;

    [Header("Looking Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 70f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    public Vector3 currentMovement;
    private float verticalRot;

    private void Start()
    {
        playerInputHandler = PlayerInputHandler.Instance;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private Vector3 CalculateWorldDir()
    {
        Vector3 inputDir = new Vector3(playerInputHandler.moveInput.x, 0, playerInputHandler.moveInput.y);
        Vector3 worldDir = transform.TransformDirection(inputDir);

        return worldDir.normalized;
    }

    private void HandleMovement()
    {
        Vector3 worldDir = CalculateWorldDir();
        currentMovement.x = worldDir.x * moveSpeed;
        currentMovement.z = worldDir.z * moveSpeed;

        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRot = Mathf.Clamp(verticalRot - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRot, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.lookInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.lookInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.AddForce(pushDir * 0.1f, ForceMode.Impulse);
        }
    }
}