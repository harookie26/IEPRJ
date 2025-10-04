using UnityEngine;
using UnityEngine.AI;

public class EnemyDistracted : EnemyState
{
    // The exact world position of the painting that distracted the enemy.
    public Vector3 PaintingPosition { get; set; }

    private NavMeshAgent agent;
    private float originalSpeed;

    public override void EnterState(EnemyStateMachine state)
    {
        // Ensure we have an agent reference
        agent = state.NavAgent;
        if (agent == null && state.Enemy != null)
            agent = state.Enemy.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning("EnemyDistracted: No NavMeshAgent available.");
            return;
        }

        // Save and set speed to the configured move speed for rushing
        originalSpeed = agent.speed;

        // Compute rush speed (fallback to MoveSpeed if multiplier invalid)
        float multiplier = state.DistractedRushMultiplier > 0f ? state.DistractedRushMultiplier : 1f;
        agent.speed = state.MoveSpeed * multiplier;

        agent.isStopped = false;

        // Set destination to the exact painting position that was interacted with
        agent.SetDestination(PaintingPosition);
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        if (agent == null)
            return;

        // Wait until the path is ready
        if (agent.pathPending)
            return;

        // If we've reached the painting (or cannot find a path), go to Calm for a few seconds
        if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            // restore agent speed
            agent.speed = originalSpeed;

            // Tell the Calm state to wait a few seconds before resuming roaming.
            // Uses the value configured on the state machine.
            state.EnemyCalm.EntryDelay = state.DistractedCalmDuration;

            state.Switchstate(state.EnemyCalm);
        }
    }

    public override void OnCollision(EnemyStateMachine state)
    {
        // No special collision handling for now.
    }
}