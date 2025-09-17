using UnityEngine;

public class EnemyChasing : EnemyState
{
    private bool playerChased = false;
    private bool playerOutside = false;

    private float chaseBufferTimer = 0;
    private float aggroRange;
    private float killRange;

    public override void EnterState(EnemyStateManager state)
    {
        aggroRange = state.EnemyAggroRadius;
        killRange = state.EnemyKillRadius;
        playerChased = true;
        playerOutside = false;
        chaseBufferTimer = 0f;
        Debug.Log("Entered Chasing State");
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
        {
            Debug.LogError("ENEMY PATHFINDING ERROR: Target or Enemy is null");
            return;
        }
        else
        {
            Vector3 playerDir = state.TargetPlayer.transform.position - state.Enemy.transform.position;
            playerDir.y = 0f;

            if (playerDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 toPlayerDir = playerDir.normalized;
            Vector3 enemyForward = state.Enemy.transform.forward;
            enemyForward.y = 0f;
            enemyForward.Normalize();

            float angleToPlayer = Vector3.Angle(enemyForward, toPlayerDir);

            Quaternion currentRotation = state.Enemy.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(toPlayerDir);

            // Rotation speed depends on angle difference
            float angleFactor = angleToPlayer / 180f; // Normalize 0 to 1
            float rotationSpeed = Mathf.Lerp(30f, 360f, angleFactor); // Adjust min/max as needed
            float maxDegrees = rotationSpeed * Time.deltaTime;

            state.Enemy.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, maxDegrees);

            // Movement is based on current forward (not directly to player)
            state.Enemy.transform.position += state.Enemy.transform.forward * state.MoveSpeed * Time.deltaTime;

            Debug.Log("Chasing player");

            float distance = Vector3.Distance(state.Enemy.transform.position, state.TargetPlayer.transform.position);
            if (distance <= killRange)
            {
                state.Switchstate(state.EnemyCalm);   /// Temporary Code to just Reset back to it's initial state.
                Debug.Log("BOOOOOOOO!");
            }

            checkPlayerOutside(state, distance);

            if (!playerChased)
            {
                Debug.Log("CHASE ENDED");
            }
        }
    }

    public override void OnCollision(EnemyStateManager state)
    {
        // Implement if needed
    }

    private void checkPlayerOutside(EnemyStateManager state, float distanceToPlayer)
    {
        if (!playerOutside)
        {
            if (distanceToPlayer > aggroRange)
            {
                playerOutside = true;
            }
        }
        else
        {
            Debug.Log($"PLAYER IS AWAY FOR {chaseBufferTimer:F2} seconds");
            chaseBufferTimer += Time.deltaTime;

            if (distanceToPlayer <= aggroRange)
            {
                playerOutside = false;
                chaseBufferTimer = 0f;
            }
            else if (chaseBufferTimer >= 5f)
            {
                playerChased = false;
            }
        }
    }
}