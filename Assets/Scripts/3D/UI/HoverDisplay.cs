using UnityEngine;
using UnityEngine.EventSystems;

public class DisplayHover : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas uiCanvas; // UI to enable when conditions are met
    [SerializeField] private float viewAngleThreshold = 30f; // degrees
    [SerializeField] private float triggerDistance = 5f;

    void Start()
    {
        if (uiCanvas != null)
            uiCanvas.enabled = false;
    }

    void Update()
    {
        Vector3 directionToObject = transform.position - mainCamera.transform.position;
        float angle = Vector3.Angle(mainCamera.transform.forward, directionToObject);

        float distance = Vector3.Distance(player.position, transform.position);

        if (angle < viewAngleThreshold && distance < triggerDistance)
        {
            uiCanvas.enabled = true;
        }
        else
        {
            uiCanvas.enabled = false;
        }
    }
}
