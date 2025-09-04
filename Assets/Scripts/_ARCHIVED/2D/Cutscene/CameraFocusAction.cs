using System;
using UnityEngine;

[Serializable]
public class CameraFocusAction : CutsceneAction
{
    public string TargetId { get; set; }
    public float Duration { get; set; } = 1.0f;
    public float CameraYOffset { get; set; } = 0f;
    public bool WaitForInput { get; set; } = true;

    public override void Execute(Action onComplete)
    {
        Debug.Log("CameraFocusAction: Executing camera focus action.");

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("CameraFocusAction: Main Camera not found.");
            onComplete();
            return;
        }

        if (string.IsNullOrEmpty(TargetId))
        {
            Debug.LogWarning("CameraFocusAction: Target ID is not set.");
            onComplete();
            return;
        }

        GameObject targetObject = GameObject.Find(TargetId);
        if (targetObject != null)
        {
            Debug.Log($"CameraFocusAction: Found target '{TargetId}' at {targetObject.transform.position}");
            Debug.Log($"[DEBUG] Camera position before move: {mainCamera.transform.position}");
            MoveCamera(mainCamera, targetObject.transform.position, onComplete);
            Debug.Log($"[DEBUG] Camera position after move: {mainCamera.transform.position}");
        }
        else
        {
            Debug.LogWarning($"CameraFocusAction: Target object with ID '{TargetId}' not found in the scene. Ensure the object is active.");
            onComplete();
        }

        foreach (var obj in GameObject.FindObjectsOfType<GameObject>())
        {
            if (obj.name == "Objective1")
                Debug.Log($"[DEBUG] Found Objective1 at {obj.transform.position}, active: {obj.activeInHierarchy}, instanceID: {obj.GetInstanceID()}");
        }
    }

    private void MoveCamera(Camera camera, Vector3 targetPosition, Action onComplete)
    {
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, targetPosition.y + CameraYOffset, camera.transform.position.z);

        Debug.Log($"[DEBUG] Target position: {targetPosition}, Final target position for camera: {finalTargetPosition}");

        var followPlayer = camera.GetComponent<FollowPlayer>();
        if (followPlayer != null)
        {
            followPlayer.enabled = false;
        }

        Action onAnimationComplete = () =>
        {
            Debug.Log($"[DEBUG] Camera actual position after animation: {camera.transform.position}");

            Action transitionBackToPlayer = () =>
            {
                if (followPlayer != null && followPlayer.player != null)
                {
                    Vector3 followPosition = followPlayer.player.position + followPlayer.offset;
                    followPosition.z = camera.transform.position.z;

                    float transitionDuration = 0.5f;
                    CameraAnimator.Animate(camera, followPosition, transitionDuration, () =>
                    {
                        followPlayer.enabled = true;
                        onComplete();
                    });
                }
                else
                {
                    onComplete();
                }
            };

            if (WaitForInput)
            {
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.StartCoroutine(InputManager.Instance.WaitForInputCoroutine(transitionBackToPlayer));
                }
                else
                {
                    Debug.LogWarning("InputManager instance not found. Cannot wait. Completing immediately.");
                    transitionBackToPlayer();
                }
            }
            else
            {
                // Wait for 2 seconds before snapping back, for example
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.StartCoroutine(WaitAndTransitionBack(3f, transitionBackToPlayer));
                }
                else
                {
                    Debug.LogWarning("InputManager instance not found. Cannot wait. Completing immediately.");
                    transitionBackToPlayer();
                }
            }
        };

        if (Duration <= 0f)
        {
            camera.transform.position = finalTargetPosition;
            onAnimationComplete();
            return;
        }

        CameraAnimator.Animate(camera, finalTargetPosition, Duration, onAnimationComplete);
    }

    private System.Collections.IEnumerator WaitAndTransitionBack(float waitTime, Action callback)
    {
        yield return new WaitForSeconds(waitTime);
        callback();
    }
}