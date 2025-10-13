using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PBManual : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 10f; // new: how fast the companion rotates toward camera

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Prevent physics from rotating the companion due to collisions while still allowing script-driven rotation.
        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // Forward / Back (W / S)
        float moveInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        // Strafe (A / D)
        float strafeInput = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;

        // Vertical (UpArrow / DownArrow) - moves in world Y independent of camera pitch
        float verticalInput = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;

        // Build horizontal movement from camera orientation but keep it level (no unintended vertical movement when camera pitches)
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude < 0.0001f) camRight = Vector3.right;
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput;
        Vector3 strafeDir = camRight * strafeInput;

        Vector3 finalDir = (moveDir + strafeDir) * moveSpeed;

        // Apply vertical movement directly on Y (separate from camera forward/back)
        finalDir.y = verticalInput * moveSpeed;

        rb.linearVelocity = finalDir;

        // Ensure physics doesn't keep rotating the rigidbody from collisions
        if (rb != null)
            rb.angularVelocity = Vector3.zero;

        // Rotation: follow camera yaw and pitch (brush tilts up/down with camera) but keep world up to avoid roll.
        Vector3 lookDir = cameraTransform.forward;
        if (lookDir.sqrMagnitude < 0.0001f)
            lookDir = Vector3.forward;

        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

        // Use transform.rotation for script-driven rotation so constraints won't block it.
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
    }
}