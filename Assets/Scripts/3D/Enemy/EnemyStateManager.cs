using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
    [SerializeField] private GameObject targetPlayer;
    [SerializeField] private GameObject enemy;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float targetingbuffer;

    [SerializeField] private float enemyAggroRadius;
    [SerializeField] private float enemyKillRadius;

    public float EnemyAggroRadius => enemyAggroRadius;
    public float EnemyKillRadius => enemyKillRadius;
    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public float MoveSpeed => moveSpeed;    
    public float TargetingBuffer => targetingbuffer;
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;

    EnemyState CurrentState;
    private EnemyChasing enemyChasing = new EnemyChasing();
    private EnemyCalm enemyCalm = new EnemyCalm();

    private void Start()
    {
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
}