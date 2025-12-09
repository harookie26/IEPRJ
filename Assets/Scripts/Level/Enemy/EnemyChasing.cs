using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static EventNames;

public class EnemyChasing : EnemyState
{
    private bool playerChased = false;
    private bool playerOutside = false;

    private float chaseBufferTimer = 0;
    private float aggroRange;
    private float killRange;

    private NavMeshAgent agent;

    // LoS debug info (updated each time HasLineOfSight is called)
    public Vector3 LastLoSOrigin { get; private set; }
    public Vector3 LastLoSTarget { get; private set; }
    public bool LastLoSClear { get; private set; }
    public Vector3 LastHitPoint { get; private set; }
    public Collider LastHitCollider { get; private set; }
    public bool HasLastLoS { get; private set; }

    public override void EnterState(EnemyStateMachine state)
    {
        // Clear any previous LoS debug state when entering chase
        ResetLoSDebug();

        aggroRange = state.EnemyAggroRadius;
        killRange = state.EnemyKillRadius;
        playerChased = true;
        playerOutside = false;
        chaseBufferTimer = 0f;

        agent = state.NavAgent;

        if (agent == null)
        {
            Debug.LogError("EnemyChasing requires a NavMeshAgent. Assign a NavMeshAgent on the EnemyStateManager or the enemy GameObject.");
            return;
        }

        agent.speed = state.MoveSpeed;
        agent.stoppingDistance = killRange;
        agent.updateRotation = true;
        agent.updatePosition = true;

        bool onNav = EnsureAgentOnNavMesh(agent);
        if (onNav)
        {
            agent.isStopped = false;
        }
        else
        {
            Debug.LogWarning("Entered Chasing State but agent is not on a NavMesh. Agent will remain inactive until NavMesh is available.");
        }

        Debug.Log("Entered Chasing State (NavMeshAgent)");
    }

    private bool EnsureAgentOnNavMesh(NavMeshAgent a)
    {
        if (a == null) return false;
        if (a.isOnNavMesh) return true;

        NavMeshHit hit;
        const float sampleRadius = 5.0f;
        if (NavMesh.SamplePosition(a.transform.position, out hit, sampleRadius, NavMesh.AllAreas))
        {
            a.Warp(hit.position);
            Debug.Log($"EnemyChasing: Warped agent to NavMesh at {hit.position}");
            return a.isOnNavMesh;
        }
        else
        {
            Debug.LogWarning("EnemyChasing: No NavMesh position found near agent. Ensure room NavMesh was baked and covers the agent position.");
            return false;
        }
    }

    public override void UpdateState(EnemyStateMachine state)
    {
        if (state.TargetPlayer == null || state.Enemy == null)
        {
            Debug.LogError("ENEMY PATHFINDING ERROR: Target or Enemy is null");
            return;
        }

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing on enemy. Cannot chase using NavMesh.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("NavMeshAgent is not on a NavMesh. Ensure the NavMesh is baked and the agent's position is on it.");
            return;
        }

        agent.speed = state.MoveSpeed;

        Vector3 targetPos = state.TargetPlayer.transform.position;
        agent.SetDestination(targetPos);

        float distanceToPlayer;
        if (agent.pathPending)
        {
            distanceToPlayer = Vector3.Distance(state.Enemy.transform.position, targetPos);
        }
        else
        {
            distanceToPlayer = agent.remainingDistance;
            if (distanceToPlayer == Mathf.Infinity)
                distanceToPlayer = Vector3.Distance(state.Enemy.transform.position, targetPos);
        }

        if (!agent.pathPending && distanceToPlayer <= killRange)
        {
            agent.isStopped = true;
            agent.ResetPath();

            Debug.Log("BOOOOOOOO! (caught by NavMeshAgent)");
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_CATCHED);
            return;
        }

        // Line-of-sight handling — stop chase after buffer when LoS is lost.
        bool hasLoS = HasLineOfSight(state);
        if (hasLoS)
        {
            // reset the buffer and continue chasing
            chaseBufferTimer = 0f;
            playerChased = true;
        }
        else
        {
            // increment buffer; if exceeded, stop chasing and go calm
            chaseBufferTimer += Time.deltaTime;
            Debug.Log($"Lost LoS for {chaseBufferTimer:F2}s (buffer {state.TargetingBuffer:F2}s)");
            if (chaseBufferTimer >= state.TargetingBuffer)
            {
                playerChased = false;
                agent.isStopped = true;
                agent.ResetPath();
                state.Switchstate(state.EnemyCalm);
                Debug.Log("CHASE ENDED (lost LoS)");
                return;
            }
        }

        // maintain original distance-away behaviour as a fallback
        checkPlayerOutside(state, Vector3.Distance(state.Enemy.transform.position, state.TargetPlayer.transform.position));

        if (!playerChased)
        {
            agent.isStopped = true;
            agent.ResetPath();
            Debug.Log("CHASE ENDED (NavMeshAgent)");
        }
        else
        {
            Debug.Log("Chasing player (NavMeshAgent)");
        }
    }

    public override void OnCollision(EnemyStateMachine state)
    {
        // Implement if needed
    }

    private void checkPlayerOutside(EnemyStateMachine state, float distanceToPlayer)
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

    private bool HasLineOfSight(EnemyStateMachine state)
    {
        HasLastLoS = false;
        LastHitCollider = null;

        if (state.TargetPlayer == null || state.Enemy == null)
        {
            LastLoSClear = false;
            return false;
        }

        Vector3 origin = state.Enemy.transform.position + Vector3.up * 1.2f;
        Vector3 targetPos = state.TargetPlayer.transform.position + Vector3.up * 1.0f; // aim for _player's approximate center
        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;
        if (dist <= 0.0001f)
        {
            // trivial clear
            LastLoSOrigin = origin;
            LastLoSTarget = targetPos;
            LastLoSClear = true;
            HasLastLoS = true;
            LastHitPoint = targetPos;
            return true;
        }

        dir /= dist;

        // store ray info for gizmos/debug
        LastLoSOrigin = origin;
        LastLoSTarget = targetPos;
        HasLastLoS = true;
        LastHitPoint = targetPos; // default to _player position

        // Ignore trigger _colliders so triggers don't block LoS.
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            LastHitPoint = hit.point;
            LastHitCollider = hit.collider;

            var hitRoot = hit.collider.transform;
            if (hitRoot == state.TargetPlayer.transform || hitRoot.IsChildOf(state.TargetPlayer.transform))
            {
                LastLoSClear = true;
                return true;
            }

            // Something else blocked the ray
            LastLoSClear = false;
            return false;
        }

        // Nothing hit between _enemy and _player -> clear LoS
        LastLoSClear = true;
        return true;
    }

    // Reset the LoS debug state so gizmos won't be stuck when not chasing.
    public void ResetLoSDebug()
    {
        HasLastLoS = false;
        LastLoSClear = false;
        LastHitCollider = null;
        LastHitPoint = Vector3.zero;
        LastLoSOrigin = Vector3.zero;
        LastLoSTarget = Vector3.zero;
    }
}