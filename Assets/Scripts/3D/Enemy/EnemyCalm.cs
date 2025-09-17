using UnityEngine;
using UnityEngine.AI;

public class EnemyCalm : EnemyState
{
    private float roamTimer;
    private float roamCooldown = 3f; 
    private Vector3 roamDestination;

    public override void EnterState(EnemyStateManager state)
    {
        roamTimer = 0f;
        roamDestination = state.Enemy.transform.position; 
        state.EnemyChasing.ResetLoSDebug();
        Debug.Log("Entered Calm State (Roaming)");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
        {
            Debug.LogError("ENEMY PATHFINDING ERROR: Target or Enemy is null");
            return;
        }

        if (state.EnemyFOV != null && state.EnemyFOV.PlayerInSight)
        {
            Debug.Log("Player detected via FOV! Switching to chase.");
            state.Switchstate(state.EnemyChasing);
            return;
        }

        roamTimer -= Time.deltaTime;
        if (roamTimer <= 0f)
        {
            roamDestination = GetRandomPoint(state.Enemy.transform.position, Random.Range(6.0f, 10.0f));
            roamTimer = roamCooldown;
            if (state.NavAgent != null && state.NavAgent.isOnNavMesh)
            {
                state.NavAgent.speed = state.PatrolSpeed;
                state.NavAgent.isStopped = false;
                state.NavAgent.SetDestination(roamDestination);
            }

        }

        Debug.Log("Enemy roaming to " + roamDestination);
    }

    public override void OnCollision(EnemyStateManager state)
    {
        
    }

    private Vector3 GetRandomPoint(Vector3 center, float range)
    {
        for (int i = 0; i < 10; i++) 
        {
            Vector3 randomPos = center + new Vector3(
                Random.Range(-range, range),
                0,
                Random.Range(-range, range)
            );

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return center; 
    }
}
