using UnityEngine;
using UnityEngine.AI;

[FoldableInspector(hideFieldHeaders: true)]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player GameObject this enemy will target.")]
    [SerializeField] private GameObject targetPlayer;
    [Tooltip("The enemy GameObject (used to cache components like the NavMeshAgent).")]
    [SerializeField] private GameObject enemy;

    [Header("Config")]
    [Tooltip("Reference to a ScriptableObject that holds tunable values (speeds, ranges, durations). " +
             "Use this to keep per-enemy data out of the MonoBehaviour and reduce script length.")]
    [SerializeField] private EnemyConfig config;

    [Header("Navigation")]
    [Tooltip("Optional: assign a NavMeshAgent here. If left empty, the agent will be cached from the 'enemy' GameObject at runtime.")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [Tooltip("Reference to the EnemyFOV component that determines line-of-sight / visibility.")]
    [SerializeField] private EnemyFOV enemyFOV;
    public EnemyFOV EnemyFOV => enemyFOV;

    private bool configWarned = false;
    private void WarnMissingConfig()
    {
        if (!configWarned)
        {
            Debug.LogWarning("EnemyStateMachine: No EnemyConfig assigned. Using fallback values. Create/assign an EnemyConfig asset to customize values.");
            configWarned = true;
        }
    }

    private float WarnAndReturn(float fallback)
    {
        WarnMissingConfig();
        return fallback;
    }

    public float MoveSpeed;
    public float PatrolSpeed;
    public float TargetingBuffer;
    public float EnemyAggroRadius;
    public float EnemyKillRadius;
    public float DistractedCalmDuration;
    public float DistractedRushMultiplier;

    [Header("Teleporting")]
    [Tooltip("Minimum seconds to wait while Calm before a teleport occurs (randomized each cycle).")]
    [SerializeField] private float teleportMinSeconds = 5f;
    [Tooltip("Maximum seconds to wait while Calm before a teleport occurs (randomized each cycle).")]
    [SerializeField] private float teleportMaxSeconds = 10f;

    // Calm-only teleport countdown
    private float teleportTimer = 0f;
    private float nextTeleportDelay = 0f;

    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;
    public NavMeshAgent NavAgent => navMeshAgent;
    public EnemyDistracted EnemyDistracted => enemyDistracted;
    public EnemyTeleporting EnemyTeleporting => enemyTeleporting;

    [Header("Enemy States")]
    EnemyState CurrentState;
    private EnemyChasing enemyChasing = new EnemyChasing();
    private EnemyCalm enemyCalm = new EnemyCalm();
    private EnemyDistracted enemyDistracted = new EnemyDistracted();
    private EnemyTeleporting enemyTeleporting = new EnemyTeleporting();

    private void Start()
    {
        if (navMeshAgent == null && enemy != null)
        {
            navMeshAgent = enemy.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogWarning("EnemyStateManager: No NavMeshAgent found on the enemy. Assign one in the inspector or add one to the enemy GameObject.");
            }
        }

        if (config != null)
        {
            MoveSpeed = config.moveSpeed;
            PatrolSpeed = config.patrolSpeed;
            TargetingBuffer = config.targetingBuffer;
            EnemyAggroRadius = config.enemyAggroRadius;
            EnemyKillRadius = config.enemyKillRadius;
            DistractedCalmDuration = config.distractedCalmDuration;
            DistractedRushMultiplier = config.distractedRushMultiplier;
        }
        else
        {
            if (!configWarned) WarnMissingConfig();
            MoveSpeed = 3f;
            PatrolSpeed = 2f;
            TargetingBuffer = 1f;
            EnemyAggroRadius = 8f;
            EnemyKillRadius = 1f;
            DistractedCalmDuration = 3f;
            DistractedRushMultiplier = 1.5f;
        }

        RollNextTeleportDelay();

        CurrentState = enemyCalm;
        CurrentState.EnterState(this);
    }

    private void Update()
    {
        // Only count down while Calm
        if (CurrentState == enemyCalm)
        {
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= nextTeleportDelay)
            {
                teleportTimer = 0f;
                RollNextTeleportDelay();
                // Enter teleport state; it teleports immediately, then returns to Calm
                Switchstate(enemyTeleporting);
            }
        }

        CurrentState?.UpdateState(this);
    }

    public void Switchstate(EnemyState state)
    {
        // Reset Calm teleport timer when entering states that interrupt Calm behaviour
        if (state == enemyChasing || state == enemyDistracted)
        {
            teleportTimer = 0f;
            RollNextTeleportDelay();
        }
        else if (state == enemyCalm)
        {
            // Each time we come back to Calm, restart the countdown
            teleportTimer = 0f;
            RollNextTeleportDelay();
        }

        CurrentState = state;
        state.EnterState(this);
    }

    private void RollNextTeleportDelay()
    {
        var min = Mathf.Max(0f, Mathf.Min(teleportMinSeconds, teleportMaxSeconds));
        var max = Mathf.Max(min, Mathf.Max(teleportMinSeconds, teleportMaxSeconds));
        nextTeleportDelay = Random.Range(min, max);
        // Debug.Log($"[EnemyStateMachine] Next Calm teleport in {nextTeleportDelay:0.00}s");
    }

    public void DistractAt(Vector3 paintingWorldPosition)
    {
        enemyDistracted.PaintingPosition = paintingWorldPosition;
        Switchstate(enemyDistracted);
    }

    private void OnDrawGizmos()
    {
        if (enemyChasing == null)
            return;

        if (CurrentState != enemyChasing)
            return;

        if (enemy == null || targetPlayer == null)
            return;

        if (!enemyChasing.HasLastLoS)
            return;

        Gizmos.color = enemyChasing.LastLoSClear ? Color.green : Color.red;
        Gizmos.DrawLine(enemyChasing.LastLoSOrigin, enemyChasing.LastLoSTarget);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(enemyChasing.LastLoSOrigin, 0.05f);
        Gizmos.DrawSphere(enemyChasing.LastLoSTarget, 0.05f);

        if (enemyChasing.LastHitCollider != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(enemyChasing.LastHitPoint, 0.12f);
        }
    }
}