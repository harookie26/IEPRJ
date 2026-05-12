using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static EventNames;

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

    [Header("Teleporting")]
    [Tooltip("List of transforms marking teleport destination points.")]
    [SerializeField] private List<Transform> teleportPoints = new List<Transform>();
    [Tooltip("Cooldown between teleports in seconds.")]
    [SerializeField] private float teleportCooldown = 0.5f;

    private CheckpointManager checkpoint => FindFirstObjectByType<CheckpointManager>();
    private bool enemyCaught = false;

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

    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public NavMeshAgent NavAgent => navMeshAgent;
    public EnemyTeleporting EnemyTeleporting => enemyTeleporting;

    public RoomComponent ForcedTeleportRoom { get; set; }

    [Header("Enemy States")]
    private EnemyTeleporting enemyTeleporting = new EnemyTeleporting();

    private bool isFrozen = false;
    private bool prevNavAgentStopped = false;
    private float prevNavAgentSpeed = 0f;
    private bool prevNavAgentEnabled = false;

    private static readonly HashSet<EnemyStateMachine> AllInstances = new HashSet<EnemyStateMachine>();

    private RoomComponent currentEnemyRoom;

    private void OnEnable()
    {
        AllInstances.Add(this);
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CATCHED, PlayerCaught);
    }

    private void OnDisable()
    {
        AllInstances.Remove(this);
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_CATCHED, PlayerCaught);
    }

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

        enemyTeleporting.SetTeleportConfig(teleportPoints, teleportCooldown);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            enemyTeleporting.ForcedRoom = null;
            enemyTeleporting.TeleportNow(this);
        }
    }

    // ----- Freeze / Unfreeze API -----

    public bool IsFrozen => isFrozen;

    public void Freeze(float duration = 0f)
    {
        if (isFrozen)
        {
            if (duration > 0f)
            {
                StopCoroutine(nameof(UnfreezeAfter));
                StartCoroutine(UnfreezeAfter(duration));
            }
            return;
        }

        isFrozen = true;

        if (navMeshAgent != null)
        {
            prevNavAgentStopped = navMeshAgent.isStopped;
            prevNavAgentSpeed = navMeshAgent.speed;
            prevNavAgentEnabled = navMeshAgent.enabled;

            if (navMeshAgent.enabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        if (duration > 0f)
        {
            StartCoroutine(UnfreezeAfter(duration));
        }
    }

    public void Unfreeze()
    {
        if (!isFrozen)
            return;

        isFrozen = false;

        if (navMeshAgent != null)
        {
            try
            {
                navMeshAgent.speed = prevNavAgentSpeed;
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = prevNavAgentStopped;
                }
            }
            catch
            {
                // ignore restore errors
            }
        }
    }

    private IEnumerator UnfreezeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Unfreeze();
    }

    public static void FreezeAll(float duration = 0f)
    {
        foreach (var e in AllInstances)
        {
            if (e != null)
                e.Freeze(duration);
        }
    }

    public static void UnfreezeAll()
    {
        foreach (var e in AllInstances)
        {
            if (e != null)
                e.Unfreeze();
        }
    }

    private void PlayerCaught()
    {
        if (enemyCaught) return;
        enemyCaught = true;

        StartCoroutine(KillSequence());
    }

    private IEnumerator KillSequence()
    {
        ///Insert Kill Animations and calls here

        yield return new WaitForSeconds(3);
        EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_GAME_RESTART);

        yield return StartCoroutine(checkpoint.ReturnToCheckpoint());

        enemyCaught = false;

    }
}