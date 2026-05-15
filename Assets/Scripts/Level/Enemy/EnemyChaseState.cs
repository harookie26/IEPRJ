using UnityEngine;
using static EventNames;

public class EnemyChaseState : EnemyState
{
    public override void EnterState(EnemyStateMachine state)
    {
        state.NavAgent.speed = state.MoveSpeed * 1.6f; // Slight speed boost during chase
        Debug.Log("Ghost is CHASING the player!");

        // Logic for Elevator Lockout can be triggered here
        // e.g., EventBroadcaster.Instance.PostEvent(GameEvents.PLAYER_CHASE_STARTED);
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        if (state.TargetPlayer == null) return;

        // 1. Move toward player
        state.NavAgent.SetDestination(state.TargetPlayer.transform.position);

        // 2. Check for Catch Distance
        float distance = Vector3.Distance(state.transform.position, state.TargetPlayer.transform.position);
        if (distance <= 1.2f) // catchDistance from your EnemyAI.cs
        {
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_CATCHED);
        }
    }

    public override void OnCollision(EnemyStateMachine state) { }
}