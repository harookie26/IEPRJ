using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.ObjectTypes;

[DisallowMultipleComponent]
[FoldableInspector]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Origin used for the interact raycast. Typically the player's camera or a head transform.")]
    public Transform rayOrigin;

    [Tooltip("Max distance for interaction raycast.")]
    public float maxDistance = 3f;

    [Tooltip("Half-angle (in degrees) of the interaction cone.")]
    [Range(0f, 90f)]
    public float coneAngle = 15f;

    [Tooltip("Number of samples used to draw the cone gizmo.")]
    [Range(3, 64)]
    public int coneGizmoSamples = 12;

    [Tooltip("Layers that can be interacted with.")]
    public LayerMask interactMask = ~0;

    [Tooltip("Layers that can be collected.")]
    public LayerMask collectMask = ~0;

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

        // First try interactables in cone
        if (TryFindClosestInCone(interactMask, out Collider interactCollider, out Vector3 interactPoint))
        {
            var interactable = interactCollider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }

        // Then try collectibles in cone
        if (TryFindClosestInCone(collectMask, out Collider collectCollider, out Vector3 collectPoint))
        {
            var collectible = collectCollider.GetComponentInParent<ICollectible>();
            if (collectible != null)
            {
                collectible.Collect();
                return;
            }
        }

        Debug.Log("Interact pressed but nothing in front to interact with.");
    }

    /// <summary>
    /// Finds the closest collider within a cone from the ray origin using an OverlapSphere + angle test.
    /// Returns true when at least one collider within the given LayerMask lies inside the cone and within maxDistance.
    /// </summary>
    private bool TryFindClosestInCone(LayerMask mask, out Collider bestCollider, out Vector3 bestPoint)
    {
        bestCollider = null;
        bestPoint = Vector3.zero;

        Vector3 origin = rayOrigin.position;
        Vector3 forward = rayOrigin.forward;
        float maxDistSqr = maxDistance * maxDistance;
        float halfAngleRad = Mathf.Deg2Rad * (coneAngle * 0.5f);
        float cosHalfAngle = Mathf.Cos(halfAngleRad);

        // Broad-phase: collect all colliders within maxDistance radius
        Collider[] candidates = Physics.OverlapSphere(origin, maxDistance, mask, QueryTriggerInteraction.Collide);
        float bestSqr = float.MaxValue;

        foreach (var col in candidates)
        {
            // use closest point on collider to origin (handles large colliders correctly)
            Vector3 closest = col.ClosestPoint(origin);
            Vector3 toPoint = closest - origin;
            float sqrMag = toPoint.sqrMagnitude;

            if (sqrMag > maxDistSqr)
                continue; // outside max distance (defensive)

            if (sqrMag <= Mathf.Epsilon)
            {
                // inside origin - consider it a hit and prefer it
                if (sqrMag < bestSqr)
                {
                    bestCollider = col;
                    bestPoint = closest;
                    bestSqr = sqrMag;
                }
                continue;
            }

            Vector3 dirToPoint = toPoint / Mathf.Sqrt(sqrMag); // normalized
            // angle test (using dot for speed)
            float dot = Vector3.Dot(forward, dirToPoint);
            if (dot >= cosHalfAngle)
            {
                if (sqrMag < bestSqr)
                {
                    bestCollider = col;
                    bestPoint = closest;
                    bestSqr = sqrMag;
                }
            }
        }

        return bestCollider != null;
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null)
            return;

        Vector3 origin = rayOrigin.position;
        Vector3 forward = rayOrigin.forward;

        // Draw central forward line (green if it would hit an interactable directly on the center ray, else red)
        Ray centerRay = new Ray(origin, forward);
        if (Physics.Raycast(centerRay, out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.05f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + forward * maxDistance);
            Gizmos.DrawWireSphere(origin + forward * maxDistance, 0.03f);
        }

        // Draw cone outline
        Gizmos.color = new Color(0f, 0.75f, 1f, 0.9f);
        int samples = Mathf.Max(3, coneGizmoSamples);
        float halfAngle = coneAngle * 0.5f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float yaw = Mathf.Lerp(-halfAngle, halfAngle, t);
            for (int j = 0; j < samples; j++)
            {
                float s = (float)j / samples;
                float pitch = Mathf.Lerp(-halfAngle, halfAngle, s);
                Quaternion rot = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
                Vector3 sampleDir = rot * forward;
                Gizmos.DrawLine(origin, origin + sampleDir.normalized * maxDistance);
            }
        }

        // Draw sphere representing broad-phase radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(origin, maxDistance);
    }
}