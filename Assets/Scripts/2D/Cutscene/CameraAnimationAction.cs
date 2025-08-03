using System;
using UnityEngine;
using DG.Tweening;

[Serializable]
public class CameraAnimationAction : CutsceneAction
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    public Vector3 StartOffset;
    public Vector3 EndOffset;
    public float StartOrthoSize = 5f;
    public float EndOrthoSize = 3.5f;
    public float Duration = 1.0f;

    public override void Execute(Action onComplete)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("CameraAnimationAction: Main Camera not found.");
            onComplete?.Invoke();
            return;
        }

        // Set initial values
        mainCamera.transform.position = StartPosition;
        mainCamera.orthographicSize = StartOrthoSize;

        var followPlayer = mainCamera.GetComponent<FollowPlayer>();
        if (followPlayer != null)
        {
            followPlayer.enabled = false;
            followPlayer.offset = StartOffset;
        }

        // DOTween sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(mainCamera.transform.DOMove(EndPosition, Duration));
        seq.Join(DOTween.To(() => mainCamera.orthographicSize, x => mainCamera.orthographicSize = x, EndOrthoSize, Duration));
        if (followPlayer != null)
        {
            seq.Join(DOTween.To(() => followPlayer.offset, x => followPlayer.offset = x, EndOffset, Duration));
        }

        seq.OnComplete(() =>
        {
            if (followPlayer != null)
                followPlayer.enabled = true;
            onComplete?.Invoke();
        });
    }
}