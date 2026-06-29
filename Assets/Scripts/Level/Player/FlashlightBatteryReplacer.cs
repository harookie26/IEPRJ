using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[FoldableInspector]
public class FlashlightBatteryReplacer : MonoBehaviour
{
    private const int MaxHits = 64;
    private static readonly RaycastHit[] RayHitBuffer = new RaycastHit[MaxHits];
    private static readonly Collider[] ColliderBuffer = new Collider[MaxHits];

    [Header("References")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Flashlight flashlight;
    [SerializeField] private PlayerCollectibleManager collectibles;
    [SerializeField] private BatteryRechargeCutscene rechargeCutscene;

    [Header("Battery Targeting")]
    [SerializeField] private LayerMask batteryMask = ~0;
    [SerializeField, Min(0.1f)] private float maxDistance = 3f;
    [SerializeField, Min(0.01f)] private float aimSphereRadius = 0.12f;

    private bool subscribed;

    private void Reset()
    {
        if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void Awake()
    {
        if (rayOrigin == null && Camera.main != null)
            rayOrigin = Camera.main.transform;

        if (flashlight == null)
            flashlight = GetComponentInChildren<Flashlight>();

        if (collectibles == null)
            collectibles = FindFirstObjectByType<PlayerCollectibleManager>();

        if (rechargeCutscene == null)
            rechargeCutscene = GetComponent<BatteryRechargeCutscene>();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
        StopReplacement();
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed || InputManager.Instance == null)
            return;

        InputManager.Instance.OnChannelStarted += HandleChannelStarted;
        InputManager.Instance.OnChannelStopped += HandleChannelStopped;
        subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!subscribed || InputManager.Instance == null)
            return;

        InputManager.Instance.OnChannelStarted -= HandleChannelStarted;
        InputManager.Instance.OnChannelStopped -= HandleChannelStopped;
        subscribed = false;
    }

    private void HandleChannelStarted()
    {
        if (!CanReplaceBattery())
            return;

        if (!TryFindBattery(out BatteryComponent battery))
            return;

        rechargeCutscene?.TryBegin(battery);
    }

    private void HandleChannelStopped()
    {
        rechargeCutscene?.Cancel();
    }

    private bool CanReplaceBattery()
    {
        if (flashlight == null || rayOrigin == null)
            return false;

        if (InputManager.Instance == null)
            return false;

        if (GameState.IsCutsceneActive || (rechargeCutscene != null && rechargeCutscene.IsPlaying))
            return false;

        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null
            && (Cursor.visible || Cursor.lockState != CursorLockMode.Locked))
        {
            return false;
        }

        if (collectibles == null)
            collectibles = FindFirstObjectByType<PlayerCollectibleManager>();

        return collectibles != null && collectibles.HasCollected("Flashlight");
    }

    private void StopReplacement()
    {
        rechargeCutscene?.Cancel();
    }

    private bool TryFindBattery(out BatteryComponent battery)
    {
        battery = null;

        if (rayOrigin == null)
            return false;

        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;
        Ray ray = new Ray(origin, direction);

        float bestDistance = float.MaxValue;
        Collider bestCollider = null;

        int rayCount = Physics.RaycastNonAlloc(ray, RayHitBuffer, maxDistance, batteryMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < rayCount; i++)
        {
            RaycastHit hit = RayHitBuffer[i];
            if (hit.collider == null)
                continue;

            BatteryComponent candidate = hit.collider.GetComponentInParent<BatteryComponent>();
            if (candidate == null || !candidate.CanUse)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestCollider = hit.collider;
            }
        }

        float closestSampleDistance = Mathf.Min(maxDistance, Mathf.Max(aimSphereRadius * 2f, 0.25f));
        float midSampleDistance = Mathf.Min(maxDistance, 1f);
        float defaultSampleDistance = Mathf.Min(maxDistance, 2f);
        float farSampleDistance = maxDistance;

        EvaluateOverlapSample(origin + direction * closestSampleDistance, closestSampleDistance);

        if (!Mathf.Approximately(midSampleDistance, closestSampleDistance))
            EvaluateOverlapSample(origin + direction * midSampleDistance, midSampleDistance);

        if (!Mathf.Approximately(defaultSampleDistance, closestSampleDistance) &&
            !Mathf.Approximately(defaultSampleDistance, midSampleDistance))
            EvaluateOverlapSample(origin + direction * defaultSampleDistance, defaultSampleDistance);

        if (!Mathf.Approximately(farSampleDistance, closestSampleDistance) &&
            !Mathf.Approximately(farSampleDistance, midSampleDistance) &&
            !Mathf.Approximately(farSampleDistance, defaultSampleDistance))
            EvaluateOverlapSample(origin + direction * farSampleDistance, farSampleDistance);

        if (bestCollider == null)
            return false;

        battery = bestCollider.GetComponentInParent<BatteryComponent>();
        return battery != null && battery.CanUse;

        void EvaluateOverlapSample(Vector3 aimPoint, float sampleDistance)
        {
            int count = Physics.OverlapSphereNonAlloc(aimPoint, aimSphereRadius, ColliderBuffer, batteryMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider col = ColliderBuffer[i];
                if (col == null)
                    continue;

                BatteryComponent candidate = col.GetComponentInParent<BatteryComponent>();
                if (candidate == null || !candidate.CanUse)
                    continue;

                Vector3 closest = col.ClosestPoint(aimPoint);
                float score = sampleDistance + Vector3.Distance(aimPoint, closest);

                if (score < bestDistance)
                {
                    bestDistance = score;
                    bestCollider = col;
                }
            }
        }
    }
}
