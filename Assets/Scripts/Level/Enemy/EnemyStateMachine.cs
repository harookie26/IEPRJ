using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;
using Game.Level;

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

    [Header("Teleporting")]
    [Tooltip("List of transforms marking teleport destination points.")]
    [SerializeField] private List<Transform> teleportPoints = new List<Transform>();
    [Tooltip("Cooldown between teleports in seconds.")]
    [SerializeField] private float teleportCooldown = 0.5f;

    [Header("Stun Configuration")]
    [SerializeField] private float stunDuration = 5f;

    [Header("Animation")]
    public Animator animator;
    
    public float StunDuration => stunDuration;

    private EnemyState currentState;

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

    public GameObject TargetPlayer => targetPlayer;
    public GameObject Enemy => enemy;
    public NavMeshAgent NavAgent => navMeshAgent;
    public EnemyTeleporting EnemyTeleporting => enemyTeleporting;

    public RoomComponent ForcedTeleportRoom { get; set; }

    [Header("Enemy States")]
    private EnemyTeleporting enemyTeleporting = new EnemyTeleporting();
    public EnemyRoamState RoamState = new EnemyRoamState();
    public EnemyChaseState ChaseState = new EnemyChaseState();

    private bool isFrozen = false;
    public bool isEnemyActivated = false;
    private int corruptedPaintingsChanneled = 0;
    //private bool prevNavAgentStopped = false;
    //private float prevNavAgentSpeed = 0f;
    //private bool prevNavAgentEnabled = false;

    private static readonly HashSet<EnemyStateMachine> AllInstances = new HashSet<EnemyStateMachine>();

    private RoomComponent currentEnemyRoom;

    private void OnEnable()
    {
        AllInstances.Add(this);
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CATCHED, PlayerCaught);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.ADD_PAINTING_RESTORED, AddRestoredPainting);
    }

    private void OnDisable()
    {
        AllInstances.Remove(this);
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_CATCHED, PlayerCaught);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.ADD_PAINTING_RESTORED, AddRestoredPainting);
    }

    private void Start()
    {
        //if (navMeshAgent == null && enemy != null)
        //{
        //    navMeshAgent = enemy.GetComponent<NavMeshAgent>();
        //    if (navMeshAgent == null)
        //    {
        //        Debug.LogWarning("EnemyStateManager: No NavMeshAgent found on the enemy. Assign one in the inspector or add one to the enemy GameObject.");
        //    }
        //}

        if (config != null)
        {
            MoveSpeed = config.moveSpeed;
            //EnemyAggroRadius = config.enemyAggroRadius;
            //EnemyKillRadius = config.enemyKillRadius;
            //DistractedCalmDuration = config.distractedCalmDuration;
            //DistractedRushMultiplier = config.distractedRushMultiplier;
        }
        else
        {
            if (!configWarned) WarnMissingConfig();
            MoveSpeed = 3f;
        }

        enemyTeleporting.SetTeleportConfig(teleportPoints, teleportCooldown);

        if (!isEnemyActivated)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.ResetPath();
            }
            return; // don't enter any state yet
        }

        ChangeState(RoamState);
    }

    private void Update()
    {
        if (!isEnemyActivated) return; //if enemy has not been activated yet

        if (isFrozen) return;

        if (currentState != null)
            currentState.UpdateState(this);

        if (currentState == RoamState)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isStunned", false);
            animator.SetBool("isRunning", false);
        }
        else if (currentState == ChaseState)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isStunned", false);
            animator.SetBool("isRunning", true);
        }
    }

    public void ChangeState(EnemyState newState)
    {
        currentState = newState;
        currentState.EnterState(this);
    }

    public Vector3 GetRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Count == 0) return transform.position;

        int index = Random.Range(0, teleportPoints.Count);
        return teleportPoints[index].position;
    }
    // ----- Freeze / Unfreeze API -----

    public void ReactToFootsteps(int roomId)
    {
        if (!isEnemyActivated) return;
        if (currentState == RoamState)
        {
            var targetRoom = RoomRegistry.GetRoom(roomId) as RoomComponent;
            if (targetRoom != null)
            {
                Vector3 warpPos = enemyTeleporting.GetForcedTeleportPoint(this, targetRoom);
                NavAgent.Warp(warpPos);
            }
        }
    }
    public bool IsFrozen => isFrozen;

    public void Freeze(float duration = 0f)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        if (isFrozen)
        {
            StopCoroutine(nameof(UnfreezeAfter));
            StartCoroutine(UnfreezeAfter(duration));
            return;
        }

        isFrozen = true;
        StartCoroutine(UnfreezeAfter(duration));
    }

    public void Unfreeze()
    {
        isFrozen = false;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = false;
        }

        ChangeState(RoamState);
    }

    private IEnumerator UnfreezeAfter(float seconds)
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isStunned", true);
        animator.SetBool("isRunning", false);
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

        yield return new WaitForSeconds(1);
        EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_GAME_RESTART);

        yield return StartCoroutine(checkpoint.ReturnToCheckpoint());

        enemyCaught = false;

    }

    public void ReactToSprinting(Vector3 playerPos)
    {
        if (!isEnemyActivated) return;
        if (isFrozen) return;
        if (currentState == ChaseState) return;

        IRoom playerRoom = RoomUtils.GetRoomForPosition(new Vector2(playerPos.x, playerPos.y));
        IRoom ghostRoom = RoomUtils.GetRoomForPosition(new Vector2(transform.position.x, transform.position.y));

        if (playerRoom != null && ghostRoom != null && playerRoom.Equals(ghostRoom))
        {
            ChangeState(ChaseState);
            NavAgent.SetDestination(playerPos);
            return;
        }

        List<(Transform point, float distance)> validPoints = new List<(Transform, float)>();

        foreach (Transform point in teleportPoints)
        {
            if (point == null) continue;

            if (Mathf.Abs(point.position.y - playerPos.y) < 2.0f)
            {
                float distX = Mathf.Abs(point.position.x - playerPos.x);
                validPoints.Add((point, distX));
            }
        }

        validPoints.Sort((a, b) => a.distance.CompareTo(b.distance));

        Transform targetPoint = null;

        if (validPoints.Count >= 2)
        {
            targetPoint = validPoints[1].point;
        }
        else if (validPoints.Count == 1)
        {
            targetPoint = validPoints[0].point;
        }

        if (targetPoint != null)
        {
            NavAgent.Warp(targetPoint.position);
            ChangeState(ChaseState);
            NavAgent.SetDestination(playerPos);
            Debug.Log($"Ghost heard sprinting! TP'd to 2nd closest point: {targetPoint.name}");
        }
    }

    private void AddRestoredPainting()
    {
        corruptedPaintingsChanneled++;

        if (corruptedPaintingsChanneled > 0 && !isEnemyActivated)
        {
            isEnemyActivated = true;

            if (navMeshAgent != null)
                navMeshAgent.isStopped = false;

            ChangeState(RoamState); // only start moving now
        }
        if (corruptedPaintingsChanneled > 1)
        {
            // method to adjust ghost aggressiveness level based on how many paintings have been restored
            AdjustEnemeyAggressiveness(corruptedPaintingsChanneled);
        }

    }

    //UNTESTED, PLAYTEST FIRST, THIS WILL NEED BALANCING//
    private void AdjustEnemeyAggressiveness(int corruptedPaintingsChanneled)
    {
        if(corruptedPaintingsChanneled == 2)
        {
            // example: increase move speed by 20%
            MoveSpeed *= 1.2f;
        }
        else if (corruptedPaintingsChanneled == 3)
        {
            // example: increase move speed by another 20%
            MoveSpeed *= 1.2f;
        }
        else if (corruptedPaintingsChanneled == 4)
        {
            // example: increase move speed by another 20%
            MoveSpeed *= 1.2f;
        }
    }
}