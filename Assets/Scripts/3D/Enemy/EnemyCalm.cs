using UnityEngine;

public class EnemyCalm : EnemyState
{
    private bool playerVisible = false;
    private float chaseBufferTimer = 0;

    public override void EnterState(EnemyStateManager state)
    {
        playerVisible = false;
        Debug.Log("Entered Calm State");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
        {
            Debug.LogError("ENEMY PATHFINDING ERROR: Target or Enemy is null");
            return;
        }

        if (getPlayerDirection(state) <= 15f)  /// if Player ever gets in front of the enemy's forward.
        {
            Debug.Log("Player is spotted.");
            state.Switchstate(state.EnemyChasing);
        }
        else
        {
            Debug.Log("Player is outside tracking cone.");
        }
    }

    public override void OnCollision(EnemyStateManager state)
    {
        // Collision logic if needed
    }

    private float getPlayerDirection(EnemyStateManager state)
    {
        // Get direction to player on the XZ plane
        Vector3 playerDir = state.TargetPlayer.transform.position - state.Enemy.transform.position;
        playerDir.y = 0f;

        if (playerDir.sqrMagnitude < 0.0001f)
            return 0;

        Vector3 toPlayerDir = playerDir.normalized;

        Vector3 enemyForward = state.Enemy.transform.forward;
        enemyForward.y = 0f;
        enemyForward.Normalize(); ///Get Enemy's XY forward direction.

        return Vector3.Angle(enemyForward, toPlayerDir); ///Get The angle from Enemy's Forward to the direction to where the player is.

    }

    

}