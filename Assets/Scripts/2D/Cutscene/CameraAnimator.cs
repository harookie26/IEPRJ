using System;
using UnityEngine;
using DG.Tweening; // Make sure DOTween is installed

public static class CameraAnimator
{
    public static void Animate(Camera camera, Vector3 targetPosition, float duration, Action onComplete)
    {
        // Kill any existing tweens on the camera's transform
        camera.transform.DOKill();

        // Animate the camera's position using DOTween
        camera.transform.DOMove(targetPosition, duration)
            .SetUpdate(UpdateType.Late) // Ensures animation runs after LateUpdate
            .OnComplete(() => onComplete?.Invoke());
    }
}