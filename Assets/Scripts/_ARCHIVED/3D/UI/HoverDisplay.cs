using UnityEngine;
using UnityEngine.EventSystems;

public class DisplayHover : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject uiCanvas;
    [SerializeField] private float viewAngleThreshold = 30f;
    [SerializeField] private float triggerDistance = 5f;

    private bool wasUIVisible = false;

    void Start()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);
        wasUIVisible = false;
    }

    void Update()
    {
        Vector3 directionToObject = transform.position - mainCamera.transform.position;
        float angle = Vector3.Angle(mainCamera.transform.forward, directionToObject);

        float distance = Vector3.Distance(player.position, transform.position);

        bool shouldShowUI = angle < viewAngleThreshold && distance < triggerDistance;

        if (shouldShowUI && !wasUIVisible)
        {
            uiCanvas.SetActive(true);
            wasUIVisible = true;
            EventBroadcaster.Instance.PostEvent(EventNames.UIEvents.HOVER_UI_SHOWN);
        }
        else if (!shouldShowUI && wasUIVisible)
        {
            uiCanvas.SetActive(false);
            wasUIVisible = false;
            EventBroadcaster.Instance.PostEvent(EventNames.UIEvents.HOVER_UI_HIDDEN);
        }
    }
}
