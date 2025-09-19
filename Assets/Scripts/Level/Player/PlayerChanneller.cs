using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.ObjectTypes;

[DisallowMultipleComponent]
public class PlayerChanneller : MonoBehaviour
{
    [Tooltip("Origin used for the interact raycast. Typically the player's camera or a head transform.")]
    public Transform rayOrigin;

    [Tooltip("Max distance for interaction raycast.")]
    public float maxDistance = 3f;

    [Tooltip("Layers that can be channeled with.")]
    public LayerMask channelMask = ~0;

    private Coroutine channelCoroutine;
    private bool subscribed;

    // Track the currently channeled target so we can stop it when we lose focus or switch targets.
    private IChannelable currentChannelTarget;

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

        // Ensure we stop the coroutine cleanly and stop any active channel target.
        if (channelCoroutine != null)
        {
            // Stop channel on current target before killing coroutine.
            if (currentChannelTarget != null)
            {
                currentChannelTarget.StopChannel();
                currentChannelTarget = null;
            }

            StopCoroutine(channelCoroutine);
            channelCoroutine = null;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChannelStarted += HandleChannelStart;
            InputManager.Instance.OnChannelStopped += HandleChannelStop;
            subscribed = true;
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
        if (rayOrigin == null)
        {
            Debug.LogWarning("PlayerChanneller: rayOrigin not set.");
            return;
        }

        // Start coroutine if not already running
        if (channelCoroutine == null)
        {
            channelCoroutine = StartCoroutine(ChannelRoutine());
        }
    }

    private void HandleChannelStop()
    {
        // Stop coroutine and stop the current target if any
        if (channelCoroutine != null)
        {
            // Stop channel on current target before killing coroutine.
            if (currentChannelTarget != null)
            {
                Debug.Log($"[PlayerChanneller] Stopping channel on current target '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' due to channel stop.");
                currentChannelTarget.StopChannel();
                currentChannelTarget = null;
            }

            StopCoroutine(channelCoroutine);
            channelCoroutine = null;
        }
    }

    private IEnumerator ChannelRoutine()
    {
        Debug.Log("[PlayerChanneller] ChannelRoutine started.");
        while (true)
        {
            // Skip UI-selected objects (same behavior as interact)
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                yield return null;
                continue;
            }

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide))
            {
                var hitTarget = hit.collider.GetComponentInParent<IChannelable>();

                if (hitTarget != null)
                {
                    // Switched to a new target
                    if (hitTarget != currentChannelTarget)
                    {
                        // Stop previous target if present
                        if (currentChannelTarget != null)
                        {
                            Debug.Log($"[PlayerChanneller] Lost focus on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' -> calling StopChannel().");
                            currentChannelTarget.StopChannel();
                        }

                        currentChannelTarget = hitTarget;
                        Debug.Log($"[PlayerChanneller] Gained focus on '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}' -> calling StartChannel().");
                        currentChannelTarget.StartChannel();
                    }
                    // else same target: do nothing (StartChannel is idempotent on the target)
                }
                else
                {
                    // Hit something that isn't channelable
                    if (currentChannelTarget != null)
                    {
                        Debug.Log($"[PlayerChanneller] Ray hit non-channelable object; stopping previous target '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}'.");
                        currentChannelTarget.StopChannel();
                        currentChannelTarget = null;
                    }
                }
            }
            else
            {
                // Ray didn't hit anything: stop current target if any
                if (currentChannelTarget != null)
                {
                    Debug.Log($"[PlayerChanneller] Ray missed; stopping current target '{(currentChannelTarget as MonoBehaviour)?.gameObject.name}'.");
                    currentChannelTarget.StopChannel();
                    currentChannelTarget = null;
                }
            }

            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        if (rayOrigin == null) return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        bool isChanneling = InputManager.Instance != null && InputManager.Instance.IsChanneling();

        Gizmos.color = isChanneling ? Color.green : Color.cyan;
        Gizmos.DrawRay(origin, dir * maxDistance);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit.point, 0.05f);
            Gizmos.DrawLine(origin, hit.point);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) return;

        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, dir * maxDistance);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, channelMask, QueryTriggerInteraction.Collide))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.06f);
            Gizmos.DrawLine(origin, hit.point);
        }
    }
}