using UnityEngine;
using UnityEngine.AI;

public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private GameObject targetPlayer;
    [SerializeField] private GameObject enemy;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float targetingbuffer;

    [SerializeField] private float enemyAggroRadius;
    [SerializeField] private float enemyKillRadius;

    // Optional: allow assigning the NavMeshAgent in the inspector.
    // If not assigned, it will be cached at runtime from the `enemy` GameObject.
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private EnemyFOV enemyFOV;
    public EnemyFOV EnemyFOV => enemyFOV;


    public float EnemyAggroRadius => enemyAggroRadius;
    public float EnemyKillRadius => enemyKillRadius;
    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public float MoveSpeed => moveSpeed;
    public float PatrolSpeed => patrolSpeed;
    public float TargetingBuffer => targetingbuffer;
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;
    public NavMeshAgent NavAgent => navMeshAgent;

    // How long the enemy should remain in Calm after being distracted (seconds).
    [SerializeField] private float distractedCalmDuration = 3f;
    public float DistractedCalmDuration => distractedCalmDuration;

    // Added distracted state
    public EnemyDistracted EnemyDistracted => enemyDistracted;

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