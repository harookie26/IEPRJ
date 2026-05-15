using UnityEngine;
using static EventNames;

public class EnemyChaseState : EnemyState
{
    private float chaseTimer = 0f;
    private const float maxChaseDuration = 10f;

    public override void EnterState(EnemyStateMachine state)
    {
        chaseTimer = 0f;

        state.NavAgent.isStopped = false;
        state.NavAgent.speed = state.MoveSpeed * 1.6f;
        Debug.Log("CHASING PLAYER - 10s limit started.");
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        chaseTimer += Time.deltaTime;

        if (chaseTimer >= maxChaseDuration)
        {
            Debug.Log("Chase timed out! Teleporting away and returning to Roam.");

            state.EnemyTeleporting.TeleportNow(state);

            state.ChangeState(state.RoamState);
            return;
        }

        if (state.TargetPlayer != null)
        {
            state.NavAgent.SetDestination(state.TargetPlayer.transform.position);
        }
    }

    public override void OnCollision(EnemyStateMachine state) { }
}