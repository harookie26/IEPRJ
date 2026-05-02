using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCam : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float verticalClamp = 70f;
    [SerializeField] private float negVerticalClamp = -55f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 euler = cameraTarget.localEulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, negVerticalClamp, verticalClamp);

        this.gameObject.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}