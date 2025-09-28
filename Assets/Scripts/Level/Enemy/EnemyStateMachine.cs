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

    public float MoveSpeed => config != null ? config.moveSpeed : WarnAndReturn(3f);
    public float PatrolSpeed => config != null ? config.patrolSpeed : WarnAndReturn(2f);
    public float TargetingBuffer => config != null ? config.targetingBuffer : WarnAndReturn(1f);
    public float EnemyAggroRadius => config != null ? config.enemyAggroRadius : WarnAndReturn(8f);
    public float EnemyKillRadius => config != null ? config.enemyKillRadius : WarnAndReturn(1f);
    public float DistractedCalmDuration => config != null ? config.distractedCalmDuration : WarnAndReturn(3f);

    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;
    public NavMeshAgent NavAgent => navMeshAgent;
    public EnemyDistracted EnemyDistracted => enemyDistracted;

    [Header("Enemy States")]
    EnemyState CurrentState;
    private EnemyChasing enemyChasing = new EnemyChasing();
    private EnemyCalm enemyCalm = new EnemyCalm();
    private EnemyDistracted enemyDistracted = new EnemyDistracted();

    private void Start()
    {
        // Cache NavMeshAgent from the enemy GameObject if not assigned in inspector
        if (navMeshAgent == null && enemy != null)
        {
            navMeshAgent = enemy.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogWarning("EnemyStateManager: No NavMeshAgent found on the enemy. Assign one in the inspector or add one to the enemy GameObject.");
            }
        }

        CurrentState = enemyCalm;
        CurrentState.EnterState(this);
    }

    private void Update()
    {
        CurrentState?.UpdateState(this);
    }

    public void Switchstate(EnemyState state)
    {
        CurrentState = state;
        state.EnterState(this);
    }

    /// <summary>
    /// Called by interactables (e.g. a painting) to distract the enemy and make it rush
    /// to the provided world position.
    /// </summary>
    public void DistractAt(Vector3 paintingWorldPosition)
    {
        enemyDistracted.PaintingPosition = paintingWorldPosition;
        Switchstate(enemyDistracted);
    }

    // Draw LoS ray and hit point for debugging in the Scene view.
    private void OnDrawGizmos()
    {
        if (enemyChasing == null)
            return;

        // Only draw gizmo while actively in the chasing state to avoid "stuck" visuals.
        if (CurrentState != enemyChasing)
            return;

        // we need the scene objects
        if (enemy == null || targetPlayer == null)
            return;

        if (!enemyChasing.HasLastLoS)
            return;

        // Draw main LoS line (green => clear, red => blocked)
        Gizmos.color = enemyChasing.LastLoSClear ? Color.green : Color.red;
        Gizmos.DrawLine(enemyChasing.LastLoSOrigin, enemyChasing.LastLoSTarget);

        // Draw small spheres for origin and target
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(enemyChasing.LastLoSOrigin, 0.05f);
        Gizmos.DrawSphere(enemyChasing.LastLoSTarget, 0.05f);

        // If there was a blocking hit, draw the hit point larger and a label via icon-ish sphere
        if (enemyChasing.LastHitCollider != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(enemyChasing.LastHitPoint, 0.12f);
        }
    }
}