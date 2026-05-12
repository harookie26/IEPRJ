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
    [Header("Proximity Settings")]
    [Tooltip("The center point for the proximity check. If left empty, defaults to this object's transform.")]
    public Transform proximityOrigin;

    [Tooltip("How close the paintbrush needs to be to a painting to channel it.")]
    public float proximityRadius = 2.5f;

    [Tooltip("Layers that contain channelable paintings.")]
    public LayerMask channelMask = ~0;

    [Tooltip("Consecutive frames a miss is tolerated before stopping the current target.")]
    public int missGraceFrames = 5;

    [Header("Re-channel control")]
    public bool requireReleaseAfterCompletion = true;
    public float rechannelCooldown = 0.35f;

    [Header("VFX Reference")]
    public GameObject paintbrushVFX;
    public Light paintbrushLight;

    [Header("Emissive Settings")]
    public MeshRenderer paintbrushRenderer;
    public string emissionPropertyName = "_EmissionColor";
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
    private HashSet<string> completedPaintingIds = new HashSet<string>();

    private void Awake()
    {
        stateMachine = GetComponent<PaintbrushStateMachine>();
        if (stateMachine == null) Debug.LogWarning("[PlayerChanneller] PlayerStateMachine not found.");

        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();
        if (collectibles == null) Debug.LogWarning("[PlayerChanneller] PlayerCollectibleManager not found.");

        if (paintbrushRenderer != null) paintbrushMaterial = paintbrushRenderer.material;
        if (proximityOrigin == null) proximityOrigin = transform; // Default to self
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

        if (paintbrushLight != null) paintbrushLight.intensity = currentEmissionIntensity;

        if (collectibles != null && collectibles.HasCollected("Paintbucket"))
        {
            if (currentEmissionIntensity > 0.001f)
            {
                paintbrushMaterial.EnableKeyword("_EMISSION");
                paintbrushMaterial.SetColor(emissionPropertyName, glowColor * currentEmissionIntensity);
                if (paintbrushVFX != null && !paintbrushVFX.activeSelf) paintbrushVFX.SetActive(true);
            }
            else
            {
                paintbrushMaterial.SetColor(emissionPropertyName, Color.black);
                paintbrushMaterial.DisableKeyword("_EMISSION");
                if (paintbrushVFX != null && paintbrushVFX.activeSelf) paintbrushVFX.SetActive(false);
            }
        }
    }

    private void OnEnable() => TrySubscribe();

    private void OnDisable()
    {
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

        if (stateMachine != null && stateMachine.IsChanneling) stateMachine.ExitChannelState();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChannelStarted += HandleChannelStart;
            InputManager.Instance.OnChannelStopped += HandleChannelStop;
            subscribed = true;

            if (InputManager.Instance.IsChanneling() && collectibles != null && collectibles.HasCollected("Paintbucket"))
                HandleChannelStart();
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
                if (InputManager.Instance.IsChanneling()) HandleChannelStart();
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
    }

    private void TryUnsubscribe()
    {
        if (!subscribed || InputManager.Instance == null) return;
        InputManager.Instance.OnChannelStarted -= HandleChannelStart;
        InputManager.Instance.OnChannelStopped -= HandleChannelStop;
        subscribed = false;
    }

    private void HandleChannelStart()
    {
        consecutiveMisses = 0;
        if (collectibles == null || !collectibles.HasCollected("Paintbucket")) return;
        if (channelCoroutine == null) channelCoroutine = StartCoroutine(ChannelRoutine());
    }

    private void HandleChannelStop()
    {
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
        if (stateMachine != null && stateMachine.IsChanneling) stateMachine.ExitChannelState();
    }

    private bool CanBeginNewChannel()
    {
        if (Time.unscaledTime < rechannelAvailableAt) return false;
        if (holdGateActive)
        {
            if (InputManager.Instance != null && InputManager.Instance.IsChanneling()) return false;
            holdGateActive = false;
        }
        if (collectibles == null || !collectibles.HasCollected("Paintbucket")) return false;
        return true;
    }

    // --- UPDATED LOGIC: OverlapSphere instead of Raycast ---
    private IEnumerator ChannelRoutine()
    {
        while (true)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                yield return null;
                continue;
            }

            // Find all colliders within the sphere
            Collider[] hits = Physics.OverlapSphere(proximityOrigin.position, proximityRadius, channelMask, QueryTriggerInteraction.Collide);

            IChannelable hitTarget = null;
            float closestDistance = float.MaxValue;

            // Loop through hits to find the closest valid IChannelable
            foreach (Collider hit in hits)
            {
                IChannelable channelable = hit.GetComponentInParent<IChannelable>();
                if (channelable != null)
                {
                    float dist = Vector3.Distance(proximityOrigin.position, hit.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        hitTarget = channelable;
                    }
                }
            }

            if (hitTarget != null)
            {
                consecutiveMisses = 0;

                if (hitTarget != currentChannelTarget)
                {
                    if (currentChannelTarget != null) currentChannelTarget.StopChannel();

                    UnsubscribeFromCompletion();

                    if (!CanBeginNewChannel())
                    {
                        currentChannelTarget = null;
                        yield return null;
                        continue;
                    }

                    currentChannelTarget = hitTarget;

                    if (stateMachine != null && !stateMachine.IsChanneling) stateMachine.EnterChannelState();

                    var mb = currentChannelTarget as MonoBehaviour;
                    currentCompletionNotifier = mb != null ? mb.GetComponent<INotifiesChannelCompletion>() : null;

                    if (currentCompletionNotifier != null)
                    {
                        string pid = currentCompletionNotifier.PaintingId;
                        if (currentCompletionNotifier.IsCompleted || (!string.IsNullOrEmpty(pid) && completedPaintingIds.Contains(pid)))
                        {
                            currentChannelTarget = null;
                            currentCompletionNotifier = null;
                            if (stateMachine != null && stateMachine.IsChanneling) stateMachine.ExitChannelState();
                            yield return null;
                            continue;
                        }
                        currentCompletionNotifier.ChannelCompleted += OnTargetCompleted;
                    }

                    currentChannelTarget.StartChannel();
                }
            }
            else
            {
                consecutiveMisses++;
                if (consecutiveMisses >= missGraceFrames && currentChannelTarget != null)
                {
                    currentChannelTarget.StopChannel();
                    currentChannelTarget = null;
                    consecutiveMisses = 0;
                    UnsubscribeFromCompletion();
                    if (stateMachine != null && stateMachine.IsChanneling) stateMachine.ExitChannelState();
                }
            }

            yield return null;
        }
    }

    private void OnTargetCompleted()
    {
        string completedId = currentCompletionNotifier?.PaintingId;

        if (currentChannelTarget != null)
        {
            currentChannelTarget.StopChannel();
            currentChannelTarget = null;
        }

        if (!string.IsNullOrEmpty(completedId) && !completedPaintingIds.Contains(completedId))
        {
            completedPaintingIds.Add(completedId);
        }

        UnsubscribeFromCompletion();
        consecutiveMisses = 0;

        if (requireReleaseAfterCompletion) holdGateActive = true;
        rechannelAvailableAt = Mathf.Max(rechannelAvailableAt, Time.unscaledTime + Mathf.Max(0f, rechannelCooldown));

        if (stateMachine != null && stateMachine.IsChanneling) stateMachine.ExitChannelState();
    }

    private void UnsubscribeFromCompletion()
    {
        if (currentCompletionNotifier != null)
        {
            currentCompletionNotifier.ChannelCompleted -= OnTargetCompleted;
            currentCompletionNotifier = null;
        }
    }

    public int GetChannelledPaintingCount() => completedPaintingIds.Count;
    public bool HasCompletedPainting(string id) => !string.IsNullOrEmpty(id) && completedPaintingIds.Contains(id);

    // --- UPDATED GIZMOS: Draw a sphere to visualize the proximity radius ---
    private void OnDrawGizmos()
    {
        Transform origin = proximityOrigin != null ? proximityOrigin : transform;

        bool isChannelingInput = InputManager.Instance != null && InputManager.Instance.IsChanneling();
        Gizmos.color = isChannelingInput ? new Color(0, 1, 0, 0.3f) : new Color(0, 1, 1, 0.3f);

        Gizmos.DrawWireSphere(origin.position, proximityRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = proximityOrigin != null ? proximityOrigin : transform;
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(origin.position, proximityRadius);
    }
}