using Game.ObjectTypes;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private UIManager uIManager;

    [Header("Channel HUD")]
    [Tooltip("Must reference the PaintbrushChanneller so we can mirror its proximity check.")]
    public PaintbrushChanneller paintbrushChanneller;

    [Header("Battery HUD")]
    [Tooltip("Layers that contain flashlight battery objects.")]
    public LayerMask batteryMask = ~0;

    private PlayerCollectibleManager collectibles;

    private string currentHudKey;

    private NarrativeInteractable currentActiveInteractable;

    private AudioSource sfxAudioSource;
    private AudioSource musicAudioSource;
    private AudioList audioList;

    // Reuse a static buffer to avoid GC from SphereCastAll/OverlapSphere allocations.
    private static readonly RaycastHit[] s_HitBuffer = new RaycastHit[64];
    private static readonly Collider[] s_ColliderBuffer = new Collider[64];

    // Ensure we reliably subscribe to InputManager even if it isn't initialized when this component is enabled
    private bool inputSubscribed = false;
    private InputManager subscribedInputManager;

    private void Reset()
    {
        if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void Awake()
    {
        if (uIManager == null)
            uIManager = FindFirstObjectByType<UIManager>();

        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject1 != null)
        {
            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();
        if (collectibles == null) Debug.LogWarning("[PlayerChanneller] PlayerCollectibleManager not found.");
    }

    private void OnEnable()
    {
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        if (inputSubscribed && subscribedInputManager != null)
        {
            subscribedInputManager.OnInteractPressed -= HandleInteract;
        }
        inputSubscribed = false;
        subscribedInputManager = null;
    }

    private void Update()
    {
        // If InputManager appears late or is replaced, keep the interact listener on the active instance.
        if (!inputSubscribed || subscribedInputManager != InputManager.Instance)
        {
            TrySubscribeInput();
        }

        UpdateInteractHud();
    }

    private void TrySubscribeInput()
    {
        if (inputSubscribed && subscribedInputManager != null)
        {
            subscribedInputManager.OnInteractPressed -= HandleInteract;
        }

        inputSubscribed = false;
        subscribedInputManager = null;

        if (InputManager.Instance == null)
            return;

        subscribedInputManager = InputManager.Instance;
        subscribedInputManager.OnInteractPressed -= HandleInteract;
        subscribedInputManager.OnInteractPressed += HandleInteract;
        inputSubscribed = true;
    }

    private void HandleInteract()
    {
        Debug.Log("[PlayerInteractor] HandleInteract called");
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            // A UI selection can survive an elevator/fade transition. It should
            // consume submit input only while the player is actually in UI mode.
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
        }

        if (rayOrigin == null)
        {
            Debug.LogWarning("PlayerInteractor: rayOrigin not set.");
            return;
        }

        if (TrySphereCastPriority(out Collider hitCol, out Vector3 hitPoint, out bool isInteract))
        {
            Debug.Log($"[PlayerInteractor] Detected collider: {hitCol.gameObject.name}, isInteract: {isInteract}");
            if (isInteract)
            {
                var interactable = FindInteractableOnCollider(hitCol);
                Debug.Log($"[PlayerInteractor] Interactable found: {interactable != null}");
                if (interactable != null)
                {
                    interactable.Interact();

                    if (interactable is NarrativeInteractable narrative)
                    {
                        currentActiveInteractable = narrative;
                    }

                    return;
                }
            }
            else
            {
                var collectible = FindCollectibleOnCollider(hitCol);
                Debug.Log($"[PlayerInteractor] Collectible found: {collectible != null}");
                if (collectible != null)
                {
                    collectible.Collect();
                    if (collectible.GetID == "Map")
                        sfxAudioSource?.PlayOneShot(audioList.paperPickupSFX);
                    else if (collectible.GetID == "Key")
                        sfxAudioSource?.PlayOneShot(audioList.keyPickupSFX);
                    else
                        sfxAudioSource?.PlayOneShot(audioList.playerCollectibleSFX);
                    return;
                }
            }
        }

        Debug.Log("Interact pressed but nothing in front to interact with.");
    }

    private void UpdateInteractHud()
    {
        if (rayOrigin == null || uIManager == null)
            return;

        string desiredKey = null;
        Collider hitCol = null;
        bool isInteract = false;

        bool hitSomething = TrySphereCastPriority(out hitCol, out _, out isInteract);

        bool isStillLookingAtCurrent = false;

        if (hitSomething && isInteract && hitCol != null)
        {
            var interactable = FindInteractableOnCollider(hitCol);
            if (interactable != null && interactable is NarrativeInteractable narrative)
            {
                if (narrative == currentActiveInteractable)
                {
                    isStillLookingAtCurrent = true;
                }
            }
        }

        if (currentActiveInteractable != null && !isStillLookingAtCurrent)
        {
            currentActiveInteractable.ClosePopUpDisplay();
            currentActiveInteractable = null;
        }


        if (hitSomething)
        {
            if (isInteract)
            {
                if (FindInteractableOnCollider(hitCol) != null)
                    desiredKey = UIManager.Keys.Interact;
            }
            else
            {
                if (FindCollectibleOnCollider(hitCol) != null)
                    desiredKey = UIManager.Keys.Interact;

                bool isUsableBattery = collectibles != null
                    && collectibles.HasCollected("Flashlight")
                    && FindBatteryOnCollider(hitCol) is BatteryComponent battery
                    && battery.CanUse;

                if (isUsableBattery)
                {
                    desiredKey = UIManager.Keys.Recharge;
                }
                else if (FindChannelableOnCollider(hitCol) != null)
                {
                    desiredKey = UIManager.Keys.Channel;
                }
            }
        }

        if (desiredKey == null && paintbrushChanneller != null && collectibles != null && collectibles.HasCollected("Paintbucket"))
        {
            Transform proximityOrigin = paintbrushChanneller.proximityOrigin != null
                ? paintbrushChanneller.proximityOrigin
                : paintbrushChanneller.transform;

            Collider[] hits = Physics.OverlapSphere(
                proximityOrigin.position,
                paintbrushChanneller.proximityRadius,
                paintbrushChanneller.channelMask,
                QueryTriggerInteraction.Collide);

            foreach (var col in hits)
            {
                var channelable = col.GetComponentInParent<IChannelable>();
                if (channelable != null)
                {
                    desiredKey = UIManager.Keys.Channel;
                    break;
                }
            }
        }
        else if (paintbrushChanneller == null)
        {
            Debug.LogWarning("[HUD] paintbrushChanneller is null — assign it in the inspector!");
        }

        if (currentActiveInteractable != null)
        {
            if (currentActiveInteractable.IsPopUpOpen)
            {
                desiredKey = null; // Force the HUD to hide if the pop-up is active
            }
        }

        if (currentHudKey == desiredKey)
            return;

        currentHudKey = desiredKey;

        if (string.IsNullOrEmpty(currentHudKey))
            uIManager.ShowHUD(string.Empty);
        else
            uIManager.ShowHUD(currentHudKey);
    }

    // Two-step approach:
    //1) Precise center raycast (RaycastNonAlloc) for exact hits.
    //2) If none found, perform an OverlapSphere (OverlapSphereNonAlloc) at an aim point along the view ray
    // to provide forgiving aim tolerance.
    // Uses NonAlloc variants and reusable buffers to avoid per-frame allocations.
    private bool TrySphereCastPriority(out Collider hitCollider, out Vector3 hitPoint, out bool isInteract)
    {
        hitCollider = null;
        hitPoint = Vector3.zero;
        isInteract = false;

        if (rayOrigin == null)
            return false;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;
        Ray ray = new Ray(origin, dir);

        int combinedMask = interactMask | collectMask | batteryMask;

        // Step1: precise center raycast
        int rayCount = Physics.RaycastNonAlloc(ray, s_HitBuffer, maxDistance, combinedMask, QueryTriggerInteraction.Collide);
        float bestInteractDist = float.MaxValue;
        float bestCollectDist = float.MaxValue;
        float bestChannelDist = float.MaxValue;
        float bestBatteryDist = float.MaxValue;
        Collider bestInteractCol = null;
        Collider bestCollectCol = null;
        Collider bestChannelCol = null;
        Collider bestBatteryCol = null;
        Vector3 bestInteractPoint = Vector3.zero;
        Vector3 bestCollectPoint = Vector3.zero;
        Vector3 bestChannelPoint = Vector3.zero;
        Vector3 bestBatteryPoint = Vector3.zero;

        for (int i = 0; i < rayCount; i++)
        {
            var h = s_HitBuffer[i];
            if (h.collider == null) continue;

            // Prefer explicit component check to avoid misclassifying by layer only.
            var interactComp = h.collider.GetComponentInParent<IInteractable>();
            if (interactComp != null)
            {
                if (!CanInteractWith(interactComp, h.collider))
                    continue;

                if (h.distance < bestInteractDist)
                {
                    bestInteractDist = h.distance;
                    bestInteractCol = h.collider;
                    bestInteractPoint = h.point;
                }
                continue;
            }

            var collectComp = h.collider.GetComponentInParent<ICollectible>();
            if (collectComp != null)
            {
                if (h.distance < bestCollectDist)
                {
                    bestCollectDist = h.distance;
                    bestCollectCol = h.collider;
                    bestCollectPoint = h.point;
                }
            }

            var channelComp = h.collider.GetComponentInParent<IChannelable>();
            if (channelComp != null)
            {
                if (h.distance < bestChannelDist)
                {
                    bestChannelDist = h.distance;
                    bestChannelCol = h.collider;
                    bestChannelPoint = h.point;
                }
            }

            var batteryComp = h.collider.GetComponentInParent<BatteryComponent>();
            if (batteryComp != null && batteryComp.CanUse)
            {
                if (h.distance < bestBatteryDist)
                {
                    bestBatteryDist = h.distance;
                    bestBatteryCol = h.collider;
                    bestBatteryPoint = h.point;
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

        if (bestBatteryCol != null)
        {
            hitCollider = bestBatteryCol;
            hitPoint = bestBatteryPoint;
            isInteract = false;
            return true;
        }

        if (bestChannelCol != null)
        {
            hitCollider = bestChannelCol;
            hitPoint = bestChannelPoint;
            isInteract = false;
            return true;
        }

        // Step2: forgiving overlap checks along the view ray. Multiple samples
        // keep interaction reliable when the camera is very close to a trigger.
        float closestSampleDistance = Mathf.Min(maxDistance, Mathf.Max(aimSphereRadius * 2f, 0.25f));
        float midSampleDistance = Mathf.Min(maxDistance, 1f);
        float defaultSampleDistance = Mathf.Min(maxDistance, 2f);
        float farSampleDistance = maxDistance;

        float bestIAimDist = float.MaxValue;
        float bestCAimDist = float.MaxValue;
        float bestChannelAimDist = float.MaxValue;
        float bestBatteryAimDist = float.MaxValue;
        Collider bestIAimCol = null;
        Collider bestCAimCol = null;
        Collider bestChannelAimCol = null;
        Collider bestBatteryAimCol = null;
        Vector3 bestIAimPoint = Vector3.zero;
        Vector3 bestCAimPoint = Vector3.zero;
        Vector3 bestChannelAimPoint = Vector3.zero;
        Vector3 bestBatteryAimPoint = Vector3.zero;

        EvaluateOverlapSample(origin + dir * closestSampleDistance, closestSampleDistance);

        if (!Mathf.Approximately(midSampleDistance, closestSampleDistance))
            EvaluateOverlapSample(origin + dir * midSampleDistance, midSampleDistance);

        if (!Mathf.Approximately(defaultSampleDistance, closestSampleDistance) &&
            !Mathf.Approximately(defaultSampleDistance, midSampleDistance))
            EvaluateOverlapSample(origin + dir * defaultSampleDistance, defaultSampleDistance);

        if (!Mathf.Approximately(farSampleDistance, closestSampleDistance) &&
            !Mathf.Approximately(farSampleDistance, midSampleDistance) &&
            !Mathf.Approximately(farSampleDistance, defaultSampleDistance))
            EvaluateOverlapSample(origin + dir * farSampleDistance, farSampleDistance);

        void EvaluateOverlapSample(Vector3 aimPoint, float sampleDistance)
        {
            int colCount = Physics.OverlapSphereNonAlloc(aimPoint, aimSphereRadius, s_ColliderBuffer, combinedMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < colCount; i++)
            {
                var col = s_ColliderBuffer[i];
                if (col == null) continue;

                Vector3 closest = col.ClosestPoint(aimPoint);
                float distToAim = Vector3.Distance(aimPoint, closest);
                float score = sampleDistance + distToAim;

                var interactComp = col.GetComponentInParent<IInteractable>();
                if (interactComp != null)
                {
                    if (!CanInteractWith(interactComp, col))
                        continue;

                    if (score < bestIAimDist)
                    {
                        bestIAimDist = score;
                        bestIAimCol = col;
                        bestIAimPoint = closest;
                    }
                    continue;
                }

                var collectComp = col.GetComponentInParent<ICollectible>();
                if (collectComp != null)
                {
                    if (score < bestCAimDist)
                    {
                        bestCAimDist = score;
                        bestCAimCol = col;
                        bestCAimPoint = closest;
                    }
                    continue;
                }

                var channelComp = col.GetComponentInParent<IChannelable>();
                if (channelComp != null)
                {
                    if (score < bestChannelAimDist)
                    {
                        bestChannelAimDist = score;
                        bestChannelAimCol = col;
                        bestChannelAimPoint = closest;
                    }
                    continue;
                }

                var batteryComp = col.GetComponentInParent<BatteryComponent>();
                if (batteryComp != null && batteryComp.CanUse)
                {
                    if (score < bestBatteryAimDist)
                    {
                        bestBatteryAimDist = score;
                        bestBatteryAimCol = col;
                        bestBatteryAimPoint = closest;
                    }
                }
            }
        }

        // Returns are OUTSIDE the loop, after all candidates are evaluated
        if (bestIAimCol != null)
        {
            hitCollider = bestIAimCol;
            hitPoint = bestIAimPoint;
            isInteract = true;
            return true;
        }

        if (bestCAimCol != null)
        {
            hitCollider = bestCAimCol;
            hitPoint = bestCAimPoint;
            isInteract = false;
            return true;
        }

        if (bestBatteryAimCol != null)
        {
            hitCollider = bestBatteryAimCol;
            hitPoint = bestBatteryAimPoint;
            isInteract = false;
            return true;
        }

        if (bestChannelAimCol != null)
        {
            hitCollider = bestChannelAimCol;
            hitPoint = bestChannelAimPoint;
            isInteract = false;
            return true;
        }

        return false;
    }

    private bool CanInteractWith(IInteractable interactable, Collider candidateCollider)
    {
        if (interactable is DrawerInteractable drawer)
        {
            return drawer.CanInteractFrom(rayOrigin);
        }

        if (interactable is LockedDoorInteractable lockedDoor)
        {
            return lockedDoor.CanInteractFrom(rayOrigin, candidateCollider, maxDistance);
        }

        return true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null)
            return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir * maxDistance);

        int combinedMask = interactMask | collectMask | batteryMask;

        // Visualize the aim sample point/sphere used by OverlapSphere fallback.
        float sampleDistance = Mathf.Min(maxDistance, 2f);
        Vector3 aimPoint = origin + dir * sampleDistance;

        // Determine if we're currently detecting an interactable/collectible and change color.
        if (TrySphereCastPriority(out Collider drawnCol, out Vector3 drawnPoint, out bool drawnIsInteract))
        {
            // Interactable -> green, collectible -> blue
            Gizmos.color = drawnIsInteract ? Color.green : Color.blue;

            // Draw line to the detected object's hit/closest point and a small marker there
            Gizmos.DrawLine(origin, drawnPoint);
            Gizmos.DrawWireSphere(drawnPoint, 0.03f);

            // Draw the aim sphere in the same color to indicate detection
            Gizmos.DrawWireSphere(aimPoint, aimSphereRadius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aimPoint, aimSphereRadius);

            // Also show a ray to the first direct hit if present (for debug)
            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, combinedMask, QueryTriggerInteraction.Collide))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.02f);
            }
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(origin, maxDistance);
    }

    // Helper that tries to find an IInteractable on the collider's object, its parents, or its children
    private static IInteractable FindInteractableOnCollider(Collider col)
    {
        if (col == null) return null;
        var comp = col.GetComponentInParent<IInteractable>();
        if (comp != null) return comp;
        return col.GetComponentInChildren<IInteractable>();
    }

    private static IChannelable FindChannelableOnCollider(Collider col)
    {
        if (col == null) return null;
        var comp = col.GetComponentInParent<IChannelable>();
        if (comp != null) return comp;
        return col.GetComponentInChildren<IChannelable>();
    }

    private static BatteryComponent FindBatteryOnCollider(Collider col)
    {
        if (col == null) return null;
        var comp = col.GetComponentInParent<BatteryComponent>();
        if (comp != null) return comp;
        return col.GetComponentInChildren<BatteryComponent>();
    }

    // Helper that tries to find an ICollectible on the collider's object, its parents, or its children
    private static ICollectible FindCollectibleOnCollider(Collider col)
    {
        if (col == null) return null;
        var comp = col.GetComponentInParent<ICollectible>();
        if (comp != null) return comp;
        return col.GetComponentInChildren<ICollectible>();
    }
}
