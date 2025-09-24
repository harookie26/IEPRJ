using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.ObjectTypes;

[DisallowMultipleComponent]
public class PaintbrushInteractor : MonoBehaviour
{
    [Tooltip("Origin used for the interact raycast. Typically the player's camera or a head transform.")]
    public Transform rayOrigin;

    [Tooltip("Max distance for interaction raycast.")]
    public float maxDistance = 3f;

    [Tooltip("Layers that can be interacted with.")]
    public LayerMask interactMask = ~0;

    private void Reset()
    {
        if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInteractPressed += HandleInteract;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInteractPressed -= HandleInteract;
    }

    private void HandleInteract()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            return;
        }

        if (rayOrigin == null)
        {
            Debug.LogWarning("PlayerInteractor: rayOrigin not set.");
            return;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }

        Debug.Log("Interact pressed but nothing in front to interact with.");
    }

    // Visualize the interact ray origin, direction and reach in the Scene view.
    private void OnDrawGizmos()
    {
        if (rayOrigin == null)
            return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        // Small marker at the origin
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(origin, 0.03f);

        // Ray showing the interaction direction and max distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir * maxDistance);

        // Wire sphere showing the max reach
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // translucent orange
        Gizmos.DrawWireSphere(origin, maxDistance);
    }
}