using Game.Level;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTeleporting : EnemyState
{
    private List<Transform> teleportPoints;
    private float teleportCooldown = 0.5f;

    private float cooldownTimer = 0f;
    private bool teleportInProgress = false;
    public RoomComponent ForcedRoom { get; set; }
    public void SetTeleportConfig(List<Transform> points, float cooldown)
    {
        teleportPoints = points;
        teleportCooldown = cooldown;
    }

    public override void EnterState(EnemyStateMachine state)
    {
        TeleportNow(state);
        cooldownTimer = teleportCooldown;
        teleportInProgress = false;
        Debug.Log("Entered Teleporting State");
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public override void OnCollision(EnemyStateMachine state)
    {
    }

    public void TeleportNow(EnemyStateMachine state)
    {
        if (state == null || state.Enemy == null)
        {
            Debug.LogError("EnemyTeleporting: State or Enemy is null");
            return;
        }

        teleportInProgress = true;

        Vector3 targetPosition = Vector3.zero;
        bool foundTarget = false;

        RoomComponent forcedRoom = ForcedRoom ?? state.ForcedTeleportRoom;

        if (forcedRoom != null)
        {
            targetPosition = GetForcedTeleportPoint(state, forcedRoom);
            foundTarget = targetPosition != Vector3.zero || forcedRoom != null;
        }

        if (!foundTarget)
        {
            targetPosition = GetRandomTeleportPoint(state);
            foundTarget = targetPosition != Vector3.zero;
        }

        if (!foundTarget)
        {
            Debug.LogWarning("EnemyTeleporting: No valid teleport target found. Teleport aborted.");
            teleportInProgress = false;
            return;
        }

        TeleportToPoint(state, targetPosition);
        teleportInProgress = false;
    }

    private Vector3 GetRandomTeleportPoint(EnemyStateMachine state)
    {
        if (teleportPoints == null || teleportPoints.Count == 0)
        {
            Debug.LogWarning("EnemyTeleporting: teleportPoints list is empty.");
            return Vector3.zero;
        }

        List<Transform> validPoints = new List<Transform>();
        foreach (var point in teleportPoints)
        {
            if (point != null)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
        {
            Debug.LogWarning("EnemyTeleporting: All teleport points are null.");
            return Vector3.zero;
        }

        IRoom currentRoom = RoomUtils.GetRoomForPosition(new Vector2(state.Enemy.transform.position.x, state.Enemy.transform.position.y));

        List<Transform> differentRoomPoints = new List<Transform>();
        foreach (var point in validPoints)
        {
            IRoom pointRoom = RoomUtils.GetRoomForPosition(new Vector2(point.position.x, point.position.y));

            if (currentRoom == null || pointRoom == null || !currentRoom.Equals(pointRoom))
            {
                differentRoomPoints.Add(point);
            }
        }

        List<Transform> pointsToUse = differentRoomPoints.Count > 0 ? differentRoomPoints : validPoints;

        Transform randomPoint = pointsToUse[Random.Range(0, pointsToUse.Count)];
        return randomPoint.position;
    }
    private Vector3 GetForcedTeleportPoint(EnemyStateMachine state, RoomComponent forcedRoom)
    {
        if (forcedRoom == null || teleportPoints == null || teleportPoints.Count == 0)
        {
            return Vector3.zero;
        }

        Bounds roomBounds = forcedRoom.Bounds;

        Transform closestPoint = null;
        float closestDistance = Mathf.Infinity;

        foreach (var point in teleportPoints)
        {
            if (point == null) continue;

            float distance = Vector3.Distance(point.position, roomBounds.center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

        if (closestPoint == null)
        {
            Debug.LogWarning("EnemyTeleporting: No valid teleport points available for forced room.");
            return Vector3.zero;
        }

        return closestPoint.position;
    }

    private void TeleportToPoint(EnemyStateMachine state, Vector3 targetPosition)
    {
        if (state == null || state.Enemy == null)
        {
            Debug.LogError("EnemyTeleporting: State or Enemy is null during teleport");
            return;
        }

        NavMeshAgent navAgent = state.NavAgent;
        GameObject enemyGameObject = state.Enemy;
        Transform enemyTransform = enemyGameObject.transform;

        float desiredY = enemyTransform.position.y;
        targetPosition.y = desiredY;

        try
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                NavMeshHit hit;
                float sampleRadius = 5f;

                if (NavMesh.SamplePosition(targetPosition, out hit, sampleRadius, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                    navAgent.ResetPath();
                    Debug.Log($"EnemyTeleporting: Warped enemy to {hit.position}");
                }
                else
                {
                    if (NavMesh.SamplePosition(targetPosition, out hit, 20f, NavMesh.AllAreas))
                    {
                        navAgent.Warp(hit.position);
                        navAgent.ResetPath();
                        Debug.Log($"EnemyTeleporting: Warped enemy to {hit.position} (fallback radius)");
                    }
                    else
                    {
                        enemyTransform.position = targetPosition;
                        Debug.LogWarning($"EnemyTeleporting: Could not find NavMesh point near {targetPosition}. Falling back to direct position set.");
                    }
                }
            }
            else
            {
                enemyTransform.position = targetPosition;
                Debug.LogWarning("EnemyTeleporting: NavMeshAgent unavailable or not on NavMesh. Using direct position.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnemyTeleporting: Exception during teleport: {ex.Message}");
            enemyTransform.position = targetPosition;
        }
    }

    public void SetForcedTeleportTarget(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("EnemyTeleporting: Attempted to set null forced teleport target.");
            return;
        }
        Debug.Log($"EnemyTeleporting: Forced teleport target set to {targetTransform.name}");
    }
}