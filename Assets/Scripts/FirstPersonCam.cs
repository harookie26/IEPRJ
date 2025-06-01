using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCam : MonoBehaviour
{
    public Transform cameraTarget;
    public float sensitivity = 2f;
    public float verticalClamp = 85f;

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
        pitch = Mathf.Clamp(pitch, -verticalClamp, verticalClamp);

        this.gameObject.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
