using UnityEngine;

public class LevelCameraCompanion : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -5);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    private Transform target;
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        if (target == null)
        {
            GameObject companion = GameObject.FindGameObjectWithTag("Companion");
            if (companion != null)
            {
                target = companion.transform;
            }
            else
            {
                Debug.LogError("No target assigned and no GameObject with 'Companion' tag found!");
                enabled = false;
                return;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationY += invertY ? mouseY : -mouseY;
        rotationY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);
        rotationX += mouseX;

        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);

        Vector3 desiredPosition = target.position + rotation * offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = rotation;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        transform.position = target.position + rotation * offset;
        transform.rotation = rotation;
    }
}