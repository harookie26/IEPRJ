using JetBrains.Annotations;
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
    [Tooltip("Minimum seconds to wait while Calm before a teleport occurs (randomized each cycle).")]
    [SerializeField] private float teleportMinSeconds = 5f;
    [Tooltip("Maximum seconds to wait while Calm before a teleport occurs (randomized each cycle).")]
    [SerializeField] private float teleportMaxSeconds = 10f;

    [Header("Detection Camera/VFX")] [Tooltip("Enable camera zoom/focus and vignette during detection sequence.")]
    [SerializeField] private bool enableCameraFxOnDetect = true;
    [Tooltip("Seconds to keep camera focused on enemy when player detected (0 = manual revert externally).")]
    [SerializeField] private float detectFocusDuration = 2.5f;
    [Tooltip("Extra zoom-in steps after initial zoom (for dramatic punch).")]
    [SerializeField] private int detectAdditionalZoomInSteps = 0;
    [Tooltip("Delay between each extra zoom step.")]
    [SerializeField] private float detectExtraZoomInterval = 0.3f;
    [Tooltip("Apply vignette increase steps (0 = single step).")]
    [SerializeField] private int detectAdditionalVignetteSteps = 0;
    [Tooltip("Delay between each extra vignette step.")]
    [SerializeField] private float detectExtraVignetteInterval = 0.25f;

    private float teleportTimer = 0f;
    private float nextTeleportDelay = 0f;

    private LevelCameraDefault levelCamera; // cached main camera behaviour
    private VFXManager vfxManager; // cached VFX manager for vignette
    private PlayerMovement playerMovement => FindFirstObjectByType<PlayerMovement>();

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
    public NavMeshAgent NavAgent => navMeshAgent; // restored property
    public EnemyChasing EnemyChasing => enemyChasing;
    public EnemyCalm EnemyCalm => enemyCalm;
    public EnemyDistracted EnemyDistracted => enemyDistracted;
    public EnemyTeleporting EnemyTeleporting => enemyTeleporting;


    public enum EnemyStateType
    {
        Calm,
        Chasing,
        Distracted,
        Teleporting,
        Unknown
    }

    public EnemyStateType CurrentStateType
    {
        get
        {
            if (CurrentState == null) return EnemyStateType.Unknown;
            if (CurrentState == enemyCalm) return EnemyStateType.Calm;
            if (CurrentState == enemyChasing) return EnemyStateType.Chasing;
            if (CurrentState == enemyDistracted) return EnemyStateType.Distracted;
            if (CurrentState == enemyTeleporting) return EnemyStateType.Teleporting;
            return EnemyStateType.Unknown;
        }
    }

    public string CurrentStateName => CurrentState?.GetType().Name ?? "Null";

    public Vector3? GetDistractedPaintingPosition()
    {
        if (CurrentState == enemyDistracted)
        {
            return enemyDistracted.PaintingPosition;
        }
        return null;
    }

    [Header("Enemy States")]
    EnemyState CurrentState;
    private EnemyChasing enemyChasing = new EnemyChasing();
    private EnemyCalm enemyCalm = new EnemyCalm();
    private EnemyDistracted enemyDistracted = new EnemyDistracted();
    private EnemyTeleporting enemyTeleporting = new EnemyTeleporting();

    private bool isFrozen = false;
    private bool prevNavAgentStopped = false;
    private float prevNavAgentSpeed = 0f;
    private bool prevNavAgentEnabled = false;

    private static readonly HashSet<EnemyStateMachine> AllInstances = new HashSet<EnemyStateMachine>();

    // Detection coroutine tracking
    private Coroutine playerDetectedRoutine; // ensures we don't stack multiple detection FX

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

        // Cache camera & VFX references
        levelCamera = FindFirstObjectByType<LevelCameraDefault>();
        vfxManager = FindFirstObjectByType<VFXManager>();

        RollNextTeleportDelay();

        CurrentState = enemyCalm;
        CurrentState.EnterState(this);
    }

    private void Update()
    {
        if (isFrozen)
            return;

        if (CurrentState == enemyCalm)
        {
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= nextTeleportDelay)
            {
                teleportTimer = 0f;
                RollNextTeleportDelay();
                Switchstate(enemyTeleporting);
            }
        }

        CurrentState?.UpdateState(this);
    }

    public void Switchstate(EnemyState state)
    {
        if (state == enemyChasing || state == enemyDistracted)
        {
            teleportTimer = 0f;
            RollNextTeleportDelay();
        }
        else if (state == enemyCalm)
        {
            teleportTimer = 0f;
            RollNextTeleportDelay();
        }

        CurrentState = state;
        state.EnterState(this);

        // Trigger PlayerDetected FX when entering chasing state.
        if (state == enemyChasing)
        {
            if (playerDetectedRoutine != null)
            {
                StopCoroutine(playerDetectedRoutine);
                playerDetectedRoutine = null;
            }
            playerDetectedRoutine = StartCoroutine(PlayerDetected());
        }
    }

    private void RollNextTeleportDelay()
    {
        var min = Mathf.Max(0f, Mathf.Min(teleportMinSeconds, teleportMaxSeconds));
        var max = Mathf.Max(min, Mathf.Max(teleportMinSeconds, teleportMaxSeconds));
        nextTeleportDelay = Random.Range(min, max);
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

            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
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
                navMeshAgent.isStopped = prevNavAgentStopped;
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

    // Detection coroutine now triggers camera focus / zoom / vignette.
    private IEnumerator PlayerDetected()
    {
        if (enableCameraFxOnDetect)
        {
            if (levelCamera == null) levelCamera = FindFirstObjectByType<LevelCameraDefault>();
            if (vfxManager == null) vfxManager = FindFirstObjectByType<VFXManager>();

            Transform enemyTransform = enemy != null ? enemy.transform : transform;

            if (levelCamera != null)
            {
                levelCamera.FocusOnEnemy(enemyTransform, detectFocusDuration);
                levelCamera.ZoomIn();
                if (detectAdditionalZoomInSteps > 0)
                    StartCoroutine(ExtraZoomSteps(levelCamera, detectAdditionalZoomInSteps, detectExtraZoomInterval));
            }
            if (vfxManager != null)
            {
                vfxManager.IncreaseVignette();
                if (detectAdditionalVignetteSteps > 0)
                    StartCoroutine(ExtraVignetteSteps(vfxManager, detectAdditionalVignetteSteps, detectExtraVignetteInterval));
            }
        }

        Time.timeScale = 0.5f;
        Freeze();
        playerMovement.SetCanMove(false);

        // Hold focus for duration
        float wait = enableCameraFxOnDetect ? detectFocusDuration : 2f;
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        Time.timeScale = 1f;
        Unfreeze();
        playerMovement.SetCanMove(true);

        // Revert FX
        if (enableCameraFxOnDetect)
        {
            if (levelCamera != null)
            {
                levelCamera.RevertFocusToPlayer();
                levelCamera.ZoomOut();
            }
            if (vfxManager != null)
            {
                vfxManager.DecreaseVignette();
            }
        }
    }

    private IEnumerator ExtraZoomSteps(LevelCameraDefault camRef, int steps, float interval)
    {
        while (steps-- > 0 && camRef != null)
        {
            yield return new WaitForSeconds(interval);
            camRef.ZoomIn();
        }
    }

    private IEnumerator ExtraVignetteSteps(VFXManager vfxRef, int steps, float interval)
    {
        while (steps-- > 0 && vfxRef != null)
        {
            yield return new WaitForSeconds(interval);
            vfxRef.IncreaseVignette();
        }
    }
}