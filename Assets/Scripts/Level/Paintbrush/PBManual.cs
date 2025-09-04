using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PBManual : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        float moveInput = Input.GetKey(KeyCode.W) ? 1f : 0f;

        Vector3 moveDir = cameraTransform.forward * moveInput;

        float strafeInput = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        Vector3 strafeDir = cameraTransform.right * strafeInput;

        Vector3 finalDir = (moveDir + strafeDir) * moveSpeed;

        rb.linearVelocity = finalDir;
    }
}