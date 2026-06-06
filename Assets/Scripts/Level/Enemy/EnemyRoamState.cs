using UnityEngine;
using UnityEngine.AI;

public class EnemyRoamState : EnemyState
{
    private float teleportTimer = 0f;
    private float nextTeleportDuration;

    public override void EnterState(EnemyStateMachine state)
    {
        teleportTimer = 0;
        nextTeleportDuration = Random.Range(5f, 8f);
        SetNewDestination(state);
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        if (state.GetComponentInChildren<EnemyFOV>().PlayerInSight)
        {
            state.ChangeState(state.ChaseState);
            return;
        }

        //teleportTimer += Time.deltaTime;
        //if (teleportTimer >= nextTeleportDuration)
        //{
        //    TeleportToNewRoom(state);
        //    teleportTimer = 0;
        //    nextTeleportDuration = Random.Range(5f, 8f);
        //}

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

    public void TeleportToNewRoom(EnemyStateMachine state)
    {
        Vector3 safeRoamTarget = state.GetRandomPointExcludingPlayerRoom();

        state.NavAgent.Warp(safeRoamTarget);

        Debug.Log("Ghost randomly teleported to a new room (Safely avoiding the player's current room).");

        //Vector3 randomPoint = state.GetRandomPoint();
        //state.NavAgent.Warp(randomPoint);
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