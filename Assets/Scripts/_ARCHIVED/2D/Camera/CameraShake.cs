using UnityEngine;
using System.Collections;
using static EventNames;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Parameters")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.1f;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CameraEvents.CAMERA_SHAKE, OnCameraShake);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CameraEvents.CAMERA_SHAKE, OnCameraShake);
    }

    private void OnCameraShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        Vector3 startPosition = transform.localPosition;
        float elapsed = 0.0f;
        float randomStartX = Random.Range(-100f, 100f);
        float randomStartY = Random.Range(-100f, 100f);

        while (elapsed < shakeDuration)
        {
            float x = Mathf.PerlinNoise(randomStartX + elapsed * 10f, 0) * 2f - 1f;
            float y = Mathf.PerlinNoise(0, randomStartY + elapsed * 10f) * 2f - 1f;

            transform.localPosition = startPosition + new Vector3(x, y, 0) * shakeMagnitude;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startPosition;
        shakeCoroutine = null;
    }
}