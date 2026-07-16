using UnityEngine;

using static EventNames;

public class EnemyChaseState : EnemyState
{
    private float chaseTimer = 0f;
    private const float maxChaseDuration = 30f;
    private const float CatchDistance = 0.75f;

    public override void EnterState(EnemyStateMachine state)
    {
        chaseTimer = 0f;

        state.NavAgent.isStopped = false;
        Debug.Log("CHASING PLAYER - 30s limit started.");
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        chaseTimer += Time.deltaTime;

        //if (chaseTimer >= maxChaseDuration)
        //{
        //    Debug.Log("Chase timed out! Teleporting away and returning to Roam.");

        //    state.EnemyTeleporting.TeleportNow(state);
        //    state.ChangeState(state.RoamState);
        //    return;
        //}

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

        if (distanceToPlayer > CatchDistance || !CanSeePlayer(state))
        {
            return;
        }

        // Do not delay a confirmed close-range catch. At sprint speed the player
        // can enter and leave this small radius before a timer completes, while
        // the ray-based FOV may also miss the player for an individual frame.
        EventBroadcaster.Instance.PostEvent(EventNames.EnemyEvents.ENEMY_CATCHED);
        Debug.Log("Ghost caught the player!");
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
