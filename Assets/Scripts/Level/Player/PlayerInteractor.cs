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

    [Header("Aim")]
    [Tooltip("Sphere radius used for aim tolerance. Larger = more forgiving.")]
    public float aimSphereRadius = 0.08f;

    [Tooltip("Layers that can be interacted with.")]
    public LayerMask interactMask = ~0;

    [Tooltip("Layers that can be collected.")]
    public LayerMask collectMask = ~0;

    [Tooltip("Optional: assign the scene UIManager in the inspector. Will FindObjectOfType if null.")]
    [SerializeField] private UIManager uiManager;

    private string currentHudKey;

    // Reuse a static buffer to avoid GC from SphereCastAll/OverlapSphere allocations.
    private static readonly RaycastHit[] s_HitBuffer = new RaycastHit[16];

    private void Reset()
    {
        if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
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

    private void Update()
    {
        UpdateInteractHud();
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

        if (TrySphereCastPriority(out Collider hitCol, out Vector3 hitPoint, out bool isInteract))
        {
            if (isInteract)
            {
                var interactable = hitCol.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    return;
                }
            }
            else
            {
                var collectible = hitCol.GetComponentInParent<ICollectible>();
                if (collectible != null)
                {
                    collectible.Collect();
                    return;
                }
            }
        }

        Debug.Log("Interact pressed but nothing in front to interact with.");
    }

    private void UpdateInteractHud()
    {
        if (rayOrigin == null || uiManager == null)
            return;

        string desiredKey = null;

        if (TrySphereCastPriority(out Collider hitCol, out _, out bool isInteract))
        {
            if (isInteract)
            {
                if (hitCol.GetComponentInParent<IInteractable>() != null)
                    desiredKey = UIManager.Keys.Interact;
            }
            else
            {
                if (hitCol.GetComponentInParent<ICollectible>() != null)
                    desiredKey = UIManager.Keys.Interact;
            }
        }

        if (currentHudKey == desiredKey)
            return;

        currentHudKey = desiredKey;

        if (string.IsNullOrEmpty(currentHudKey))
            uiManager.ShowHUD(string.Empty);
        else
            uiManager.ShowHUD(currentHudKey);
    }

    // Forward SphereCast with a small radius, identical in spirit to PlayerChanneller.
    // Uses NonAlloc variant and a reusable buffer to avoid per-frame allocations.
    // Prioritizes interactable hits over collectible hits; selects the nearest within each category.
    private bool TrySphereCastPriority(out Collider hitCollider, out Vector3 hitPoint, out bool isInteract)
    {
        hitCollider = null;
        hitPoint = Vector3.zero;
        isInteract = false;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;
        Ray ray = new Ray(origin, dir);

        int combinedMask = interactMask | collectMask;
        int count = Physics.SphereCastNonAlloc(ray, aimSphereRadius, s_HitBuffer, maxDistance, combinedMask, QueryTriggerInteraction.Collide);
        if (count <= 0)
            return false;

        float bestInteractDist = float.MaxValue;
        float bestCollectDist = float.MaxValue;
        Collider bestInteractCol = null;
        Collider bestCollectCol = null;
        Vector3 bestInteractPoint = Vector3.zero;
        Vector3 bestCollectPoint = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            var h = s_HitBuffer[i];
            if (h.collider == null) continue;

            int layerBit = 1 << h.collider.gameObject.layer;

            // Interactable first
            if ((interactMask.value & layerBit) != 0)
            {
                if (h.distance < bestInteractDist)
                {
                    bestInteractDist = h.distance;
                    bestInteractCol = h.collider;
                    bestInteractPoint = h.point;
                }
                continue;
            }

            // Then collectible
            if ((collectMask.value & layerBit) != 0)
            {
                if (h.distance < bestCollectDist)
                {
                    bestCollectDist = h.distance;
                    bestCollectCol = h.collider;
                    bestCollectPoint = h.point;
                }
            }
        }

        if (bestInteractCol != null)
        {
            hitCollider = bestInteractCol;
            hitPoint = bestInteractPoint;
            isInteract = true;
            return true;
        }

        if (bestCollectCol != null)
        {
            hitCollider = bestCollectCol;
            hitPoint = bestCollectPoint;
            isInteract = false;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null)
            return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir * maxDistance);

        int combinedMask = interactMask | collectMask;
        if (Physics.SphereCast(origin, aimSphereRadius, dir, out RaycastHit hit, maxDistance, combinedMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, aimSphereRadius);
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(origin, maxDistance);
    }
}