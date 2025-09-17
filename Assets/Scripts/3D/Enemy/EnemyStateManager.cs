using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour
{
    [SerializeField] private GameObject targetPlayer;
    [SerializeField] private GameObject enemy;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float targetingbuffer;

    [SerializeField] private float enemyAggroRadius;
    [SerializeField] private float enemyKillRadius;

    // Optional: allow assigning the NavMeshAgent in the inspector.
    // If not assigned, it will be cached at runtime from the `enemy` GameObject.
    [SerializeField] private NavMeshAgent navMeshAgent;

    public float EnemyAggroRadius => enemyAggroRadius;
    public float EnemyKillRadius => enemyKillRadius;
    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public float MoveSpeed => moveSpeed;    
    public float TargetingBuffer => targetingbuffer;
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;
    public NavMeshAgent NavAgent => navMeshAgent;

    EnemyState CurrentState;
    private EnemyChasing enemyChasing = new EnemyChasing();
    private EnemyCalm enemyCalm = new EnemyCalm();

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