using UnityEngine;
using UnityEngine.AI;

public class EnemyTeleporting : EnemyState
{
    public override void EnterState(EnemyStateMachine state)
    {
        DoTeleport(state);
        // After teleporting, go back to Calm. EnemyStateMachine will roll a new Calm timer.
        state.Switchstate(state.EnemyCalm);
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        // No-op: teleport happens immediately on enter.
    }

    public override void OnCollision(EnemyStateMachine state)
    {
        // No-op
    }

    private void DoTeleport(EnemyStateMachine state)
    {
        var rooms = Object.FindObjectsOfType<RoomComponent>();
        if (rooms == null || rooms.Length == 0)
        {
            Debug.LogWarning("[EnemyTeleporting] No RoomComponent found. Teleport skipped.");
            return;
        }

        var agent = state.NavAgent;
        var enemyGO = state.Enemy != null ? state.Enemy : state.gameObject;
        var currentPos = enemyGO.transform.position;

        // Identify current room (if any) to prefer a different one
        RoomComponent currentRoom = null;
        foreach (var r in rooms)
        {
            var b = r.Bounds;
            if (b.size.sqrMagnitude > Mathf.Epsilon && b.Contains(currentPos))
            {
                currentRoom = r;
                break;
            }
        }

        RoomComponent[] candidates;
        if (currentRoom != null && rooms.Length > 1)
            candidates = System.Array.FindAll(rooms, r => r != currentRoom);
        else
            candidates = rooms;

        const int maxAttempts = 16;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var room = candidates[Random.Range(0, candidates.Length)];
            var bounds = room.Bounds;
            if (bounds.size.sqrMagnitude <= Mathf.Epsilon) continue;

            var randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (agent != null && agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(randomPoint, out var hit, 2.0f, NavMesh.AllAreas))
                {
                    if (agent.Warp(hit.position))
                    {
                        Debug.Log($"[EnemyTeleporting] Teleported via NavMesh to {hit.position} in room '{room.name}' (Id={room.Id}).");
                        return;
                    }
                }
            }

            enemyGO.transform.position = randomPoint;
            Debug.Log($"[EnemyTeleporting] Teleported (direct) to {randomPoint} in room '{room.name}' (Id={room.Id}).");
            return;
        }

        Debug.LogWarning("[EnemyTeleporting] Failed to find a valid teleport point after several attempts.");
    }
}