using UnityEngine;

public class TrailFollowDynamic : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private bool startAttached = true;
    [SerializeField] private bool destroyOnReach = true;
    [SerializeField] private float stopDistance = 0.1f;

    // Lifetime settings: trail will disappear when this time elapses after launch
    [SerializeField] private bool useLifetime = true;
    [SerializeField] private float maxLifetime = 5f;

    // If true the trail will automatically start moving at Start()
    [SerializeField] private bool autoLaunchAtStart = true;

    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;
    [SerializeField] private Transform target3;
    [SerializeField] private Transform target4;
    [SerializeField] private Transform target5;
    [SerializeField] private Transform target6;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    bool launched = false;
    float timer = 0f;
    private Transform target;

    // lifetime timer (counts only while launched)
    private float lifeTimer = 0f;

    private TrailRenderer trailRenderer;

    // keep a reference to the manager we subscribed to so we can unsubscribe reliably
    private PlayerCollectibleManager collectibleManagerRef;

    void Start()
    {
        target = target1;

        trailRenderer = GetComponent<TrailRenderer>();

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && startAttached)
        {
            transform.position = player.position;
        }

        // Do NOT emit at startup to avoid the trail following the _player while walking.
        if (trailRenderer != null)
            trailRenderer.emitting = false;

        // Disable updates until explicitly started via Restart()/Launch()
        launched = false;
        timer = 0f;
        lifeTimer = 0f;
        enabled = false;

        // Attempt to subscribe in Start too (covers cases where this component enabled before the manager's Awake)
        TrySubscribeToCollectibleManager();

        // Optionally auto-launch at Start if requested in the Inspector
        if (autoLaunchAtStart)
        {
            // If there's no delay, start immediately; otherwise enable Update so delay countdown begins
            if (delay <= 0f)
                Launch();
            else
                enabled = true;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[TrailFollowDynamic] Start: target set to '{(target != null ? target.name : "<null>")}', autoLaunch={autoLaunchAtStart}, launched={launched}, position={transform.position}");
            if (target != null)
                Debug.Log($"[TrailFollowDynamic] target position: {target.position}");
        }
    }

    void OnEnable()
    {
        // Subscribe to collectible events (OnEnable may run before manager Awake)
        TrySubscribeToCollectibleManager();
    }

    void OnDisable()
    {
        if (collectibleManagerRef != null)
        {
            collectibleManagerRef.CollectibleAdded -= OnCollectibleAdded;
            collectibleManagerRef = null;
        }
    }

    private void TrySubscribeToCollectibleManager()
    {
        if (collectibleManagerRef != null)
            return;

        // Prefer the static Instance if available
        var mgr = PlayerCollectibleManager.Instance ?? FindFirstObjectByType<PlayerCollectibleManager>();
        if (mgr != null)
        {
            collectibleManagerRef = mgr;
            collectibleManagerRef.CollectibleAdded += OnCollectibleAdded;

            // If paintbucket already collected, apply behavior immediately
            if (collectibleManagerRef.HasCollected("Paintbucket"))
            {
                OnCollectibleAdded("Paintbucket");
            }
        }
    }

    private void OnCollectibleAdded(string id)
    {
        if (enableDebugLogs) Debug.Log($"[TrailFollowDynamic] OnCollectibleAdded: {id}");

        if (string.Equals(id, "Paintbucket", System.StringComparison.Ordinal))
        {
            // switch to target2 when Paintbucket is collected
            ChangeTarget(2);

            // If not currently launched, reposition (if attached) and launch so the trail moves to the new target immediately
            if (!launched)
            {
                if (player != null && startAttached)
                    transform.position = player.position;

                // ensure emitting and start moving
                if (trailRenderer != null)
                    trailRenderer.emitting = true;

                Launch();
            }
        }
    }

    void Update()
    {
        if (!launched)
        {
            if (enableDebugLogs) Debug.Log($"[TrailFollowDynamic] Update: not launched. delay={delay}, timer={timer}");

            if (delay > 0f)
            {
                if (player != null && startAttached)
                    transform.position = player.position;

                timer += Time.deltaTime;
                if (timer >= delay) Launch();
            }
            return;
        }

        // count lifetime while launched
        if (useLifetime)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime)
            {
                // Instead of destroying, stop emitting and stop movement so the trail can be reused.
                StopEmittingAndStop();
                return;
            }
        }

        if (target == null)
        {
            if (enableDebugLogs) Debug.Log("[TrailFollowDynamic] Update: target is null");
            return;
        }

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        if (enableDebugLogs) Debug.Log($"[TrailFollowDynamic] Update: launched, target='{target.name}', dist={dist}, stopDistance={stopDistance}");

        if (dist <= stopDistance)
        {
            if (destroyOnReach)
            {
                // Previously destroyed here; now stop emitting and stop movement so the object persists.
                StopEmittingAndStop();
                return;
            }

            // Instead of destroying the GameObject, stop movement and let external code restart it.
            launched = false;
            timer = 0f;
            enabled = false;
            // leave transform at target
            transform.position = target.position;
            return;
        }

        transform.position += dir.normalized * moveSpeed * Time.deltaTime;

        // rotate to face the movement direction so the trail points toward the target
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // call this to start the trail moving from the _player toward the target
    public void Launch()
    {
        launched = true;
        enabled = true;
        // reset lifetime counter
        lifeTimer = 0f;

        if (trailRenderer != null)
            trailRenderer.emitting = true;

        if (enableDebugLogs) Debug.Log($"[TrailFollowDynamic] Launch called. launched={launched}, target='{(target!=null?target.name:"<null>")}', pos={transform.position}");
    }

    // Reset state and optionally reposition to _player so the trail can be restarted multiple times
    public void Restart()
    {
        launched = false;
        timer = 0f;
        lifeTimer = 0f;

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && startAttached)
            transform.position = player.position;

        // Ensure component is enabled so Update runs
        enabled = true;

        if (trailRenderer != null)
            trailRenderer.emitting = true;

        if (delay <= 0f)
            Launch();
    }

    // Stop the trail movement and reset internal state
    public void Stop()
    {
        launched = false;
        timer = 0f;
        lifeTimer = 0f;
        // leave transform as-is; component can be disabled if desired
        enabled = false;

        if (trailRenderer != null)
            trailRenderer.emitting = false;
    }

    public void ChangeTarget(int targetIndex)
    {
        switch (targetIndex)
        {
            case 1:
                target = target1;
                break;
            case 2:
                target = target2;
                break;
            case 3:
                target = target3;
                break;
            case 4:
                target = target4;
                break;
            case 5:
                target = target5;
                break;
            case 6:
                target = target6;
                break;
            default:
                Debug.LogWarning("Invalid target index: " + targetIndex);
                break;
        }

        if (enableDebugLogs) Debug.Log($"[TrailFollowDynamic] ChangeTarget -> {targetIndex}. new target='{(target!=null?target.name:"<null>")}'");
    }

    // Stop emission and disable movement so the trail GameObject never gets destroyed and can be reused.
    private void StopEmittingAndStop()
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        // Reset movement state so the object can be restarted later
        launched = false;
        lifeTimer = 0f;
        timer = 0f;
        enabled = false;

        if (enableDebugLogs) Debug.Log("[TrailFollowDynamic] StopEmittingAndStop called");
    }
}
