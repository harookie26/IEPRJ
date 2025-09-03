using UnityEngine;
using System.Collections;

public class LevelCamera : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public float zoomedFOV = 30f;    // Target field of view for zoom in (smaller = more zoomed)
    public float zoomDuration = 1f;  // Duration of zoom transition

    private Camera cam;
    private float defaultFOV;
    private Vector3 originalOffset;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        cam = GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
        originalOffset = offset;
    }

    private void LateUpdate()
    {
        Vector3 targetPos = new Vector3(player.position.x, player.position.y, 0) + offset;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
        transform.position = smoothedPos;

        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void HallwayZoomIn()
    {
        Vector3 targetOffset = originalOffset + new Vector3(0, -1.0f, 0);
        StartCoroutine(ZoomAndOffsetCoroutine(zoomedFOV, targetOffset, zoomDuration));
        Debug.Log("Zooming In");
    }

    public void HallwayZoomOut()
    {
        StartCoroutine(ZoomAndOffsetCoroutine(defaultFOV, originalOffset, zoomDuration));
    }

    private IEnumerator ZoomAndOffsetCoroutine(float targetFOV, Vector3 targetOffset, float duration)
    {
        float startFOV = cam.fieldOfView;
        Vector3 startOffset = offset;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            offset = Vector3.Lerp(startOffset, targetOffset, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.fieldOfView = targetFOV;
        offset = targetOffset;
    }
}
