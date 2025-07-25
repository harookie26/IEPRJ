using System;
using UnityEngine;

[Serializable]
public class CameraFocusAction : CutsceneAction
{
    public string TargetId { get; set; }
    public float Duration { get; set; } = 1.0f;
    public float CameraZ { get; set; } = -10f;
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
            MoveCamera(mainCamera, targetObject.transform.position, onComplete);
        }
        else
        {
            Debug.LogWarning($"CameraFocusAction: Target object with ID '{TargetId}' not found in the scene. Ensure the object is active.");
            onComplete();
        }
    }

    private void MoveCamera(Camera camera, Vector3 targetPosition, Action onComplete)
    {
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, targetPosition.y, CameraZ);

        var followPlayer = camera.GetComponent<FollowPlayer>();
        if (followPlayer != null)
        {
            followPlayer.enabled = false;
        }

        Action onAnimationComplete = () =>
        {
            Action transitionBackToPlayer = () =>
            {
                if (followPlayer != null && followPlayer.player != null)
                {
                    // Smoothly animate back to the player's position
                    Vector3 followPosition = followPlayer.player.position + followPlayer.offset;
                    followPosition.z = camera.transform.position.z; // Preserve Z if needed

                    // Use CameraAnimator to animate back
                    float transitionDuration = 0.5f; // You can adjust this duration as needed
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
                    Debug.LogWarning("InputManager instance not found. Cannot wait for input. Completing immediately.");
                    transitionBackToPlayer();
                }
            }
            else
            {
                transitionBackToPlayer();
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
}