using UnityEngine;
using System.Collections;

public class HallwayTrigger : MonoBehaviour
{
    public float zoomedFOV = 30f;    // Target field of view for zoom in (smaller = more zoomed)
    public float zoomDuration = 1f;  // Duration of zoom transition

    public Vector3 offset;
    private float defaultFOV;
    private Vector3 originalOffset;
    private Camera cam;

    private void Start()
    {
        LevelCameraDefault levelCameraDefault = FindFirstObjectByType<LevelCameraDefault>();
        if (levelCameraDefault != null)
        {
            offset = levelCameraDefault.GetOffset();
            originalOffset = levelCameraDefault.offset;
        }
        else
        {
            offset = Vector3.zero;
            originalOffset = Vector3.zero;
            Debug.LogWarning("LevelCameraDefault instance not found.");
        }

        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           HallwayZoomIn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HallwayZoomOut();
        }
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