using UnityEngine;

// Attach this to the trail (the GameObject that has the TrailRenderer).
// Behavior: when enabled the trail starts at the player's position, then moves toward the referenced target,
// leaving a trail behind. You can configure speed, start delay, and whether to destroy the trail on reach.
public class TrailFollowDynamic : MonoBehaviour
{
    public Transform player; // the source the trail starts from
    public Transform target; // the object the trail should point / move to

    public float moveSpeed = 10f;
    public float delay = 0f; // delay before launching from the player
    public bool startAttached = true; // keep trail at player position until launch
    public bool destroyOnReach = true; // kept for compatibility but we won't call Destroy so it can be restarted
    public float stopDistance = 0.1f;

    bool launched = false;
    float timer = 0f;

    void Start()
    {
        // If player isn't assigned try to find an object tagged "Player"
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && startAttached)
        {
            transform.position = player.position;
        }

        // Do NOT auto-launch at Start so the trail can be controlled by external scripts (Restart/Launch)
        // if (delay <= 0f) Launch();
    }

    void Update()
    {
        if (!launched)
        {
            if (delay > 0f)
            {
                if (player != null && startAttached)
                    transform.position = player.position;

                timer += Time.deltaTime;
                if (timer >= delay) Launch();
            }
            return;
        }

        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        if (dist <= stopDistance)
        {
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

    // call this to start the trail moving from the player toward the target
    public void Launch()
    {
        launched = true;
        enabled = true;
    }

    // Reset state and optionally reposition to player so the trail can be restarted multiple times
    public void Restart()
    {
        launched = false;
        timer = 0f;

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && startAttached)
            transform.position = player.position;

        // Ensure component is enabled so Update runs
        enabled = true;

        if (delay <= 0f)
            Launch();
    }

    // Stop the trail movement and reset internal state
    public void Stop()
    {
        launched = false;
        timer = 0f;
        // leave transform as-is; component can be disabled if desired
        enabled = false;
    }
}
