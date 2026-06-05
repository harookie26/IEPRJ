using UnityEngine;

using static EventNames;

public class EnemyChaseState : EnemyState
{
    private float chaseTimer = 0f;
    private const float maxChaseDuration = 30f;
    private const float catchConfirmDuration = 0.25f;
    private float catchDistance = 1.2f;
    private float catchConfirmTimer = 0f;

    public override void EnterState(EnemyStateMachine state)
    {
        chaseTimer = 0f;
        catchConfirmTimer = 0f;

        state.NavAgent.isStopped = false;
        Debug.Log("CHASING PLAYER - 30s limit started.");
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

        if (state.TargetPlayer == null)
        {
            return;
        }

        if (state.NavAgent != null)
        {
            state.NavAgent.SetDestination(state.TargetPlayer.transform.position);
        }

        Vector3 enemyPosition = GetEnemyPosition(state);
        Vector3 playerPosition = state.TargetPlayer.transform.position;
        float distanceToPlayer = Vector3.Distance(enemyPosition, playerPosition);

        if (distanceToPlayer > catchDistance || !CanSeePlayer(state))
        {
            catchConfirmTimer = 0f;
            return;
        }

        catchConfirmTimer += Time.deltaTime;

        if (catchConfirmTimer >= catchConfirmDuration)
        {
            EventBroadcaster.Instance.PostEvent(EventNames.EnemyEvents.ENEMY_CATCHED);
            Debug.Log("Ghost caught the player!");
        }
    }

    private Vector3 GetEnemyPosition(EnemyStateMachine state)
    {
        if (state.NavAgent != null && state.NavAgent.enabled)
        {
            return state.NavAgent.transform.position;
        }

        if (state.Enemy != null)
        {
            return state.Enemy.transform.position;
        }

        return state.transform.position;
    }

    private bool CanSeePlayer(EnemyStateMachine state)
    {
        EnemyFOV fov = state.GetComponentInChildren<EnemyFOV>();
        return fov == null || fov.PlayerInSight;
    }

    public override void OnCollision(EnemyStateMachine state) { }
}
