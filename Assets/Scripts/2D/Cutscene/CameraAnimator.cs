using System;
using UnityEngine;

public class CameraAnimator : MonoBehaviour
{
    private Camera _camera;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _duration;
    private Action _onComplete;
    private float _elapsedTime;

    public static void Animate(Camera camera, Vector3 targetPosition, float duration, Action onComplete)
    {
        // Ensure there isn't an old animator on the camera
        var oldAnimator = camera.GetComponent<CameraAnimator>();
        if (oldAnimator != null)
        {
            Destroy(oldAnimator);
        }

        var animator = camera.gameObject.AddComponent<CameraAnimator>();
        animator.Initialize(camera, targetPosition, duration, onComplete);
    }

    private void Initialize(Camera camera, Vector3 targetPosition, float duration, Action onComplete)
    {
        _camera = camera;
        _startPosition = camera.transform.position;
        _targetPosition = targetPosition;
        _duration = duration;
        _onComplete = onComplete;
        _elapsedTime = 0f;
    }

    void Update()
    {
        if (_elapsedTime < _duration)
        {
            _elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _duration);
            _camera.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
        }
        else
        {
            _camera.transform.position = _targetPosition;
            _onComplete?.Invoke();
            Destroy(this); // Animation is complete, remove this component
        }
    }
}