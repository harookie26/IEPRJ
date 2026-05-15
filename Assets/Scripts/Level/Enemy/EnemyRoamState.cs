using UnityEngine;
using UnityEngine.AI;

public class EnemyRoamState : EnemyState
{
    private float teleportTimer = 0f;
    private float nextTeleportDuration;

    public override void EnterState(EnemyStateMachine state)
    {
        teleportTimer = 0;
        nextTeleportDuration = Random.Range(8f, 12f);
        SetNewDestination(state);
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        // 1. Detection (FOV)
        if (state.GetComponentInChildren<EnemyFOV>().PlayerInSight)
        {
            state.ChangeState(state.ChaseState);
            return;
        }

        // 2. Teleport Timer (Only counts while NOT frozen)
        teleportTimer += Time.deltaTime;
        if (teleportTimer >= nextTeleportDuration)
        {
            TeleportToNewRoom(state);
            teleportTimer = 0;
            nextTeleportDuration = Random.Range(8f, 12f);
        }

        // 3. Movement Check
        if (!state.NavAgent.pathPending && state.NavAgent.remainingDistance <= state.NavAgent.stoppingDistance)
        {
            SetNewDestination(state);
        }
    }

    private void SetNewDestination(EnemyStateMachine state)
    {
        Vector3 target = state.GetRandomPoint();
        state.NavAgent.SetDestination(target);
    }

    private void TeleportToNewRoom(EnemyStateMachine state)
    {
        // Since you aren't using the Teleporting script, 
        // use NavMesh.Warp to move to one of your points
        Vector3 randomPoint = state.GetRandomPoint();
        state.NavAgent.Warp(randomPoint);
    }

    private void ResetTeleportTimer()
    {
        nextTeleportDuration = Random.Range(8f, 12f);
    }

    private void MoveToNewPoint(EnemyStateMachine state)
    {
        Vector3 nextPoint = state.GetRandomPoint();
        if (nextPoint != Vector3.zero)
        {
            state.NavAgent.SetDestination(nextPoint);
        }
    }

    public override void OnCollision(EnemyStateMachine state) { }
}