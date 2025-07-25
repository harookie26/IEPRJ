using System;
using UnityEngine;

[Serializable]
public class CameraResetAction : CutsceneAction
{
    public override void Execute(Action onComplete)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var followPlayer = mainCamera.GetComponent<FollowPlayer>();
            if (followPlayer != null)
            {
                followPlayer.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("CameraResetAction: Main Camera not found.");
        }

        onComplete();
    }
}