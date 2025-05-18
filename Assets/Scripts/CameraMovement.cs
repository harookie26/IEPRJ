using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform player; // Reference to the player
    public Vector3 offset = new Vector3(0, 0, -2); // Camera position relative to player
    public float mouseSensitivity = 3.0f; // Sensitivity for mouse movement

    private float currentYaw = 0f;

    void Start()
    {
        // Ensure the camera starts at the fixed center rotation
        currentYaw = 0f;
    }

    void LateUpdate()
    {
        // Get horizontal mouse movement
        float mouseX = Input.GetAxis("Mouse X");
        currentYaw += mouseX * mouseSensitivity;

        // Rotate the offset around the Y axis
        Quaternion rotation = Quaternion.Euler(0, currentYaw, 0);
        Vector3 rotatedOffset = rotation * offset;

        // Set camera position and look at the player
        transform.position = player.position + rotatedOffset;
        transform.LookAt(player);
    }
}
