using System;
using UnityEngine;

[Serializable]
public class CameraFocusAction : CutsceneAction
{
    public Transform target;
    public float cameraZ = -10f; // Default Z position for a 2D camera

    public override void Execute(Action onComplete)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && target != null)
        {
            mainCamera.transform.position = new Vector3(target.position.x, target.position.y, cameraZ);
        }
        else
        {
            Debug.LogWarning("Main Camera or Target is not set for CameraFocusAction.");
        }
        
        onComplete(); // This action completes instantly
    }
}