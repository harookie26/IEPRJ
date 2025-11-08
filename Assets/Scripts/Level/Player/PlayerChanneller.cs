using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.ObjectTypes;

[DisallowMultipleComponent]
[FoldableInspector]
public class PlayerChanneller : MonoBehaviour
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

    private PlayerCollectibleManager collectibles;

    private Coroutine channelCoroutine;
    private bool subscribed;

    private IChannelable currentChannelTarget;

    private INotifiesChannelCompletion currentCompletionNotifier;

    private PlayerStateMachine stateMachine;

    private int consecutiveMisses;

    private bool holdGateActive;
    private float rechannelAvailableAt;

    public int ChannelledPaintingCount = 0;

    private void Awake()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine == null)
        {
            Debug.LogWarning("[PlayerChanneller] PlayerStateMachine not found on the same GameObject. ChannelState transitions will be skipped.");
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

        return true;
    }

    private IEnumerator ChannelRoutine()
    {
        Debug.Log("[PlayerChanneller] ChannelRoutine started.");
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
                    if (currentCompletionNotifier != null)
                    {
                        currentCompletionNotifier.ChannelCompleted += OnTargetCompleted;
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
        if (currentChannelTarget != null)
        {
            Debug.Log($"[PlayerChanneller] Target completed -> StopChannel on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}'.");
            currentChannelTarget.StopChannel();
            currentChannelTarget = null;
        }

        UnsubscribeFromCompletion();
        consecutiveMisses = 0;

        if (requireReleaseAfterCompletion)
            holdGateActive = true;

        rechannelAvailableAt = Mathf.Max(rechannelAvailableAt, Time.unscaledTime + Mathf.Max(0f, rechannelCooldown));

        if (stateMachine != null && stateMachine.IsChanneling)
            stateMachine.ExitChannelState();

        ChannelledPaintingCount++;
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
        return ChannelledPaintingCount;
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