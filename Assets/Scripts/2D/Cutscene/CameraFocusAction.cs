using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class CameraFocusAction : CutsceneAction
{
    public string targetId;
    public float duration = 1.0f; // Duration of the camera movement
    public float cameraZ = -10f; // Default Z position for a 2D camera

    public override void Execute(Action onComplete)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("CameraFocusAction: Main Camera not found.");
            onComplete();
            return;
        }

        if (string.IsNullOrEmpty(targetId))
        {
            Debug.LogWarning("CameraFocusAction: Target ID is not set.");
            onComplete();
            return;
        }

        GameObject targetObject = GameObject.Find(targetId);
        if (targetObject != null)
        {
            MoveCamera(mainCamera, targetObject.transform.position, onComplete);
        }
        else
        {
            Debug.LogWarning($"CameraFocusAction: Target object with ID '{targetId}' not found in the scene. Ensure the object is active.");
            onComplete();
        }
    }

    private void MoveCamera(Camera camera, Vector3 targetPosition, Action onComplete)
    {
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, targetPosition.y, cameraZ);

        // If duration is 0, just snap to the target position
        if (duration <= 0f)
        {
            camera.transform.position = finalTargetPosition;
            onComplete?.Invoke();
            return;
        }

        // Use a MonoBehaviour to handle the animation over time
        CameraAnimator.Animate(camera, finalTargetPosition, duration, onComplete);
    }
}