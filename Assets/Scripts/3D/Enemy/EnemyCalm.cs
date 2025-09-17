using UnityEngine;

public class EnemyCalm : EnemyState
{
    private bool playerVisible = false;
    private float chaseBufferTimer = 0;

    public override void EnterState(EnemyStateManager state)
    {
        playerVisible = false;
        // Clear LoS debug kept by the chasing state so gizmos reset when we go calm.
        state.EnemyChasing.ResetLoSDebug();
        Debug.Log("Entered Calm State");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
        {
            Debug.LogError("ENEMY PATHFINDING ERROR: Target or Enemy is null");
            return;
        }

        // If player is directly in front (narrow cone) -> spot and chase.
        if (getPlayerDirection(state) <= 15f)
        {
            Debug.Log("Player is spotted (in cone).");
            state.Switchstate(state.EnemyChasing);
            return;
        }

        // Also allow spotting if player is within aggro radius AND has unobstructed LoS.
        float distToPlayer = Vector3.Distance(state.TargetPlayer.transform.position, state.Enemy.transform.position);
        if (distToPlayer <= state.EnemyAggroRadius && HasLineOfSight(state))
        {
            Debug.Log("Player is spotted (LoS + aggro range).");
            state.Switchstate(state.EnemyChasing);
            return;
        }

        Debug.Log("Player is outside tracking cone and/or occluded.");
    }

    public override void OnCollision(EnemyStateManager state)
    {
        // Collision logic if needed
    }

    private float getPlayerDirection(EnemyStateManager state)
    {
        // Get direction to player on the XZ plane
        Vector3 playerDir = state.TargetPlayer.transform.position - state.Enemy.transform.position;
        playerDir.y = 0f;

        if (playerDir.sqrMagnitude < 0.0001f)
            return 0;

        Vector3 toPlayerDir = playerDir.normalized;

        Vector3 enemyForward = state.Enemy.transform.forward;
        enemyForward.y = 0f;
        enemyForward.Normalize(); ///Get Enemy's XY forward direction.

        return Vector3.Angle(enemyForward, toPlayerDir); ///Get The angle from Enemy's Forward to the direction to where the player is.

    }

    // Local LoS check used by the calm state so the enemy can spot the player even if not facing them.
    private bool HasLineOfSight(EnemyStateManager state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
            return false;

        Vector3 origin = state.Enemy.transform.position + Vector3.up * 1.2f;
        Vector3 targetPos = state.TargetPlayer.transform.position + Vector3.up * 1.0f;
        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;
        if (dist <= 0.0001f) return true;

        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var hitRoot = hit.collider.transform;
            if (hitRoot == state.TargetPlayer.transform || hitRoot.IsChildOf(state.TargetPlayer.transform))
                return true;

            return false;
        }

        return true;
    }
}