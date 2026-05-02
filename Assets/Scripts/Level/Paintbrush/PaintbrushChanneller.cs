using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.ObjectTypes;
using System.Collections.Generic;
using Assets.Scripts.Level.Paintbrush;

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintbrushChanneller : MonoBehaviour
{
    [Tooltip("Origin used for the interact raycast. Typically the player's camera or a head transform.")]
    public Transform rayOrigin;

    [Tooltip("Max distance for interaction raycast.")]
    public float maxDistance = 3f;

    [Tooltip("Layers that can be channeled with.")]
    public LayerMask channelMask = ~0;

    [Header("Channel Aim")]
    [Tooltip("Sphere radius used for aim tolerance. Larger = more forgiving.")]
    public float aimSphereRadius = 0.08f;
    [Tooltip("Consecutive frames a miss is tolerated before stopping the current target.")]
    public int missGraceFrames = 5;

    [Header("Re-channel control")]
    [Tooltip("Require releasing the channel key before another channel can start after completion.")]
    public bool requireReleaseAfterCompletion = true;

    [Tooltip("Delay after completion before a new channel can start (even if the key is still held). Uses unscaled time.")]
    public float rechannelCooldown = 0.35f;

    [Header("VFX Reference")]
    [Tooltip("The VFX/Particle System prefab parented to the paintbrush.")]
    public GameObject paintbrushVFX;

    [Header("Light Reference")]
    [Tooltip("The point light parented to the paintbrush VFX Object.")]
    public Light paintbrushLight;

    [Header("Emissive Settings")]
    [Tooltip("The MeshRenderer of the paintbrush part that should glow.")]
    public MeshRenderer paintbrushRenderer;

    [Tooltip("The name of the emissive color property in your shader (usually _EmissionColor).")]
    public string emissionPropertyName = "_EmissionColor";

    [Tooltip("The color and max intensity of the glow.")]
    [ColorUsage(true, true)]

    public Color glowColor = Color.white;
    public float emissionFadeSpeed = 5f;

    private Material paintbrushMaterial;
    private float currentEmissionIntensity = 0f;

    private PlayerCollectibleManager collectibles;

    private Coroutine channelCoroutine;
    private bool subscribed;

    private IChannelable currentChannelTarget;

    private INotifiesChannelCompletion currentCompletionNotifier;

    private PaintbrushStateMachine stateMachine;

    private int consecutiveMisses;

    private bool holdGateActive;
    private float rechannelAvailableAt;

    // Track completed painting IDs so each painting only completes once.
    private HashSet<string> completedPaintingIds = new HashSet<string>();


    private void Awake()
    {
        stateMachine = GetComponent<PaintbrushStateMachine>();
        if (stateMachine == null)
        {
            Debug.LogWarning("[PlayerChanneller] PlayerStateMachine not found on the same GameObject. ChannelState transitions will be skipped.");
        }

        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();
        if (collectibles == null)
        {
            Debug.LogWarning("[PlayerChanneller] PlayerCollectibleManager not found. Channeling will be disabled until present.");
        }

        if (paintbrushRenderer != null)
        {
            paintbrushMaterial = paintbrushRenderer.material;
        }
    }

    private void Update()
    {
        bool isInputActive = InputManager.Instance != null && InputManager.Instance.IsChanneling();

        float targetIntensity = isInputActive ? 3f : 0f;

        currentEmissionIntensity = Mathf.MoveTowards(
            currentEmissionIntensity,
            targetIntensity,
            Time.deltaTime * emissionFadeSpeed
        );

        if (paintbrushLight != null)
        {
            paintbrushLight.intensity = currentEmissionIntensity;
        }

        if (collectibles.HasCollected("Paintbucket"))
        {
            if (currentEmissionIntensity > 0.001f)
            {
                paintbrushMaterial.EnableKeyword("_EMISSION");
                paintbrushMaterial.SetColor(emissionPropertyName, glowColor * currentEmissionIntensity);

                if (paintbrushVFX != null && !paintbrushVFX.activeSelf)
                    paintbrushVFX.SetActive(true);
            }
            else
            {
                paintbrushMaterial.SetColor(emissionPropertyName, Color.black);
                paintbrushMaterial.DisableKeyword("_EMISSION");

                if (paintbrushVFX != null && paintbrushVFX.activeSelf)
                    paintbrushVFX.SetActive(false);
            }
        }
    }

    private void Reset()
    {
        if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        // Keep this one, it's good practice to kill visuals if the script dies
        if (paintbrushVFX != null) paintbrushVFX.SetActive(false);

        TryUnsubscribe();

        if (channelCoroutine != null)
        {
            if (currentChannelTarget != null)
            {
                currentChannelTarget.StopChannel();
                currentChannelTarget = null;
            }

            StopCoroutine(channelCoroutine);
            channelCoroutine = null;
        }

        UnsubscribeFromCompletion();

        consecutiveMisses = 0;
        holdGateActive = false;
        rechannelAvailableAt = 0f;

        if (stateMachine != null && stateMachine.IsChanneling)
            stateMachine.ExitChannelState();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChannelStarted += HandleChannelStart;
            InputManager.Instance.OnChannelStopped += HandleChannelStop;
            subscribed = true;

            if (InputManager.Instance.IsChanneling())
            {
                if (collectibles != null && collectibles.HasCollected("Paintbucket"))
                    HandleChannelStart();
            }
        }
        else
        {
            StartCoroutine(DelayedSubscribe());
        }
    }

    private IEnumerator DelayedSubscribe()
    {
        float timeout = 2f;
        float t = 0f;
        while (!subscribed && t < timeout)
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnChannelStarted += HandleChannelStart;
                InputManager.Instance.OnChannelStopped += HandleChannelStop;
                subscribed = true;

                if (InputManager.Instance.IsChanneling())
                {
                    HandleChannelStart();
                }
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        if (!subscribed)
            Debug.LogWarning($"[PlayerChanneller] Could not find InputManager to subscribe within {timeout}s on '{name}'.");
    }

    private void TryUnsubscribe()
    {
        if (!subscribed) return;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChannelStarted -= HandleChannelStart;
            InputManager.Instance.OnChannelStopped -= HandleChannelStop;
        }

        subscribed = false;
        Debug.Log($"[PlayerChanneller] Unsubscribed from InputManager on '{name}'.");
    }

    private void HandleChannelStart()
    {
        consecutiveMisses = 0;

        // Block channeling unless Paintbucket collected
        if (collectibles == null || !collectibles.HasCollected("Paintbucket"))
        {
            Debug.Log("[PlayerChanneller] Channel blocked: Paintbucket not collected.");
            return;
        }

        if (rayOrigin == null)
        {
            Debug.LogWarning("PlayerChanneller: rayOrigin not set. Targeting disabled.");
            return;
        }

        if (channelCoroutine == null)
        {
            channelCoroutine = StartCoroutine(ChannelRoutine());
        }
    }

    private void HandleChannelStop()
    {
        // Removed abrupt paintbrushVFX.SetActive(false) here

        if (channelCoroutine != null)
        {
            if (currentChannelTarget != null)
            {
                Debug.Log($"[PlayerChanneller] Stopping channel on current target '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' due to input stop.");
                currentChannelTarget.StopChannel();
                currentChannelTarget = null;
            }

            StopCoroutine(channelCoroutine);
            channelCoroutine = null;
        }

        UnsubscribeFromCompletion();

        consecutiveMisses = 0;

        holdGateActive = false;

        if (stateMachine != null && stateMachine.IsChanneling)
            stateMachine.ExitChannelState();
    }

    private bool CanBeginNewChannel()
    {
        if (Time.unscaledTime < rechannelAvailableAt)
            return false;

        if (holdGateActive)
        {
            if (InputManager.Instance != null && InputManager.Instance.IsChanneling())
                return false;

            holdGateActive = false;
        }

        // Require collectible for starting new channel
        if (collectibles == null || !collectibles.HasCollected("Paintbucket"))
            return false;

        return true;
    }

    private IEnumerator ChannelRoutine()
    {
        Debug.Log("[PlayerChanneller] ChannelRoutine started.");

        // Removed abrupt paintbrushVFX.SetActive(true) here

        while (true)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                yield return null;
                continue;
            }

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

            bool gotHit = Physics.SphereCast(ray, aimSphereRadius, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide);
            IChannelable hitTarget = gotHit ? hit.collider.GetComponentInParent<IChannelable>() : null;

            if (hitTarget != null)
            {
                consecutiveMisses = 0;

                if (hitTarget != currentChannelTarget)
                {
                    if (currentChannelTarget != null)
                    {
                        Debug.Log($"[PlayerChanneller] Lost focus on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' -> StopChannel().");
                        currentChannelTarget.StopChannel();
                    }

                    UnsubscribeFromCompletion();

                    if (!CanBeginNewChannel())
                    {
                        currentChannelTarget = null;
                        yield return null;
                        continue;
                    }

                    currentChannelTarget = hitTarget;

                    if (stateMachine != null && !stateMachine.IsChanneling)
                        stateMachine.EnterChannelState();

                    var mb = currentChannelTarget as MonoBehaviour;
                    currentCompletionNotifier = mb != null ? mb.GetComponent<INotifiesChannelCompletion>() : null;

                    // If this painting is already completed, skip starting a channel on it.
                    if (currentCompletionNotifier != null)
                    {
                        string pid = currentCompletionNotifier.PaintingId;
                        if (currentCompletionNotifier.IsCompleted || (!string.IsNullOrEmpty(pid) && completedPaintingIds.Contains(pid)))
                        {
                            Debug.Log($"[PlayerChanneller] Painting (ID={pid}) already completed. Skipping channel.");
                            currentChannelTarget = null;
                            currentCompletionNotifier = null;
                            if (stateMachine != null && stateMachine.IsChanneling)
                                stateMachine.ExitChannelState();
                            yield return null;
                            continue;
                        }

                        // Subscribe to completion event
                        currentCompletionNotifier.ChannelCompleted += OnTargetCompleted;

                    }
                    else
                    {
                        // No completion notifier available on the target. Start channeling but it won't notify completion.
                        Debug.LogWarning($"[PlayerChanneller] Target '{mb?.gameObject.name}' does not implement INotifiesChannelCompletion. Channeling may not complete properly.");
                    }

                    Debug.Log($"[PlayerChanneller] Gained focus on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' -> StartChannel().");
                    currentChannelTarget.StartChannel();
                }
            }
            else
            {
                bool keepCurrent = false;

                if (currentChannelTarget != null)
                {
                    var mb = currentChannelTarget as MonoBehaviour;
                    if (mb != null)
                    {
                        var targetCols = mb.GetComponentsInChildren<Collider>();
                        if (targetCols != null && targetCols.Length > 0)
                        {
                            var hits = Physics.SphereCastAll(ray, aimSphereRadius, maxDistance, channelMask, QueryTriggerInteraction.Collide);
                            for (int i = 0; i < hits.Length && !keepCurrent; i++)
                            {
                                var hCol = hits[i].collider;
                                for (int j = 0; j < targetCols.Length; j++)
                                {
                                    if (hCol == targetCols[j])
                                    {
                                        keepCurrent = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (keepCurrent)
                {
                    consecutiveMisses = 0;
                }
                else
                {
                    consecutiveMisses++;
                    if (consecutiveMisses >= missGraceFrames && currentChannelTarget != null)
                    {
                        Debug.Log($"[PlayerChanneller] Lost channel (miss {consecutiveMisses} frames) -> StopChannel on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}'.");
                        currentChannelTarget.StopChannel();
                        currentChannelTarget = null;
                        consecutiveMisses = 0;

                        // Removed abrupt paintbrushVFX.SetActive(false) here

                        UnsubscribeFromCompletion();

                        // Exit ChannelState because channeling ended (no active target)
                        if (stateMachine != null && stateMachine.IsChanneling)
                            stateMachine.ExitChannelState();
                    }
                }
            }

            yield return null;
        }
    }

    private void OnTargetCompleted()
    {
        string completedId = null;
        if (currentCompletionNotifier != null)
        {
            completedId = currentCompletionNotifier.PaintingId;
        }

        if (currentChannelTarget != null)
        {
            Debug.Log($"[PlayerChanneller] Target completed -> StopChannel on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}'.");
            currentChannelTarget.StopChannel();
            currentChannelTarget = null;
        }

        // Record completion ID before unsubscribing
        if (!string.IsNullOrEmpty(completedId))
        {
            if (!completedPaintingIds.Contains(completedId))
            {
                completedPaintingIds.Add(completedId);
                Debug.Log($"[PlayerChanneller] Recorded completed painting ID {completedId}. Total completed: {completedPaintingIds.Count}");
            }
            else
            {
                Debug.Log($"[PlayerChanneller] Painting ID {completedId} was already recorded as completed.");
            }
        }

        UnsubscribeFromCompletion();
        consecutiveMisses = 0;

        if (requireReleaseAfterCompletion)
            holdGateActive = true;

        rechannelAvailableAt = Mathf.Max(rechannelAvailableAt, Time.unscaledTime + Mathf.Max(0f, rechannelCooldown));

        if (stateMachine != null && stateMachine.IsChanneling)
            stateMachine.ExitChannelState();
    }

    private void UnsubscribeFromCompletion()
    {
        if (currentCompletionNotifier != null)
        {
            currentCompletionNotifier.ChannelCompleted -= OnTargetCompleted;
            currentCompletionNotifier = null;
        }
    }

    public int GetChannelledPaintingCount()
    {
        return completedPaintingIds.Count;
    }

    public bool HasCompletedPainting(string id)
    {
        return !string.IsNullOrEmpty(id) && completedPaintingIds.Contains(id);
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null) return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        bool isChannelingInput = InputManager.Instance != null && InputManager.Instance.IsChanneling();

        Gizmos.color = isChannelingInput ? Color.green : Color.cyan;
        Gizmos.DrawRay(origin, dir * maxDistance);

        if (Physics.SphereCast(origin, aimSphereRadius, dir, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, aimSphereRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, dir * maxDistance);

        if (Physics.SphereCast(origin, aimSphereRadius, dir, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, aimSphereRadius);
        }
    }
}