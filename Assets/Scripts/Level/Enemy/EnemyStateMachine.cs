using Game.Level;
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

    [Header("Teleporting")]
    [Tooltip("List of transforms marking teleport destination points.")]
    [SerializeField] public List<Transform> teleportPoints = new List<Transform>();
    [Tooltip("Cooldown between teleports in seconds.")]
    [SerializeField] private float teleportCooldown = 0.5f;

    [Header("Stun Configuration")]
    [SerializeField] private float stunDuration = 5f;

    [Header("Animation")]
    public Animator animator;

    [Header("Stun Glitch Effects")]
    [Tooltip("The minimum duration (seconds) a single blink state lasts.")]
    [SerializeField] private float minGlitchInterval = 0.35f;
    [Tooltip("The maximum duration (seconds) a single blink state lasts.")]
    [SerializeField] private float maxGlitchInterval = 0.55f;

    private Coroutine stunGlitchCoroutine;

    [Header("Stun Configuration")]
    [Tooltip("Stun durations indexed by how many paintings are restored (0, 1, 2, 3+ paintings).")]
    [SerializeField] private float[] stunDurationTiers = new float[] { 5f, 3.5f, 2.5f, 1.5f };
    private float currentStunDuration;

    [Header("Tension Management")]
    [SerializeField] private float maxSilentPassiveDuration = 45f;
    private float passiveTensionTimer = 0f;
    public float StunDuration => currentStunDuration;

    private EnemyState currentState;

    private CheckpointManager checkpoint => FindFirstObjectByType<CheckpointManager>();
    private bool enemyCaught = false;

    private bool configWarned = false;
    private int scriptedEncounters = 0;

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

    private float baseRoamSpeed;
    private float baseChaseSpeed;

    private bool isFrozen = false;
    public bool isEnemyActivated = false;
    private int corruptedPaintingsChanneled = 0;

    private static readonly HashSet<EnemyStateMachine> AllInstances = new HashSet<EnemyStateMachine>();

    private RoomComponent currentEnemyRoom;

    private Vector3 initialEnemyPosition;
    private Vector3 initialEnemyRotation;

    private bool hasInitialized = false;
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
        currentStunDuration = stunDurationTiers[0];
        initialEnemyPosition = enemy.transform.position;
        initialEnemyRotation = enemy.transform.eulerAngles;

        if (config != null)
        {
            MoveSpeed = config.moveSpeed;
            baseRoamSpeed = config.patrolSpeed;
            baseChaseSpeed = config.moveSpeed;
        }
        else
        {
            if (!configWarned) WarnMissingConfig();
            MoveSpeed = 3f;
        }

        enemyTeleporting.SetTeleportConfig(teleportPoints, teleportCooldown);
        hasInitialized = true;

        // Controlled exclusively via the EnemyManager setup loop now
        if (!isEnemyActivated)
        {
            DisableAgentPhysics();
            return;
        }

        ChangeState(RoamState);
    }

    public void SetActiveGhost(bool dynamicState)
    {
        if (!hasInitialized) isEnemyActivated = dynamicState;

        isEnemyActivated = dynamicState;

        if (isEnemyActivated)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;
            }

            SetGhostVisualsAndPhysics(true);

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                ChangeState(RoamState);
                Debug.Log($"[{gameObject.name}] Awoken and revealed by Manager. Commencing Floor Roam.");
            }
            else
            {
                StartCoroutine(DelayedRoamActivation());
            }
        }
        else
        {
            StopCoroutine(nameof(UnfreezeAfter));
            StopCoroutine(nameof(DelayedRoamActivation));

            if (stunGlitchCoroutine != null)
            {
                StopCoroutine(stunGlitchCoroutine);
                stunGlitchCoroutine = null;
            }

            DisableAgentPhysics(); 
            SetGhostVisuals(false);
            Debug.Log($"[{gameObject.name}] Put to sleep and hidden by Manager.");
        }
    }
    private IEnumerator DelayedRoamActivation()
    {
        yield return null; 
        if (isEnemyActivated && navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            ChangeState(RoamState);
            Debug.Log($"[{gameObject.name}] Delayed Awaken successful after frame correction.");
        }
    }

    private void SetGhostVisualsAndPhysics(bool visible)
    {
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = visible;
        }

        AudioSource ghostVoice = enemy.GetComponent<AudioSource>();
        if (ghostVoice != null)
        {
            if (visible) ghostVoice.Play();
            else ghostVoice.Stop();
        }
    }

    private void DisableAgentPhysics()
    {
        StopChaseAudio();
        if (navMeshAgent != null)
        {
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.ResetPath();
            }

            navMeshAgent.enabled = false;
        }
    }

    private void Update()
    {
        if (!isEnemyActivated) return; 

        if (isFrozen) return;

        if (currentState != null)
            currentState.UpdateState(this);

        if (currentState == RoamState)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("isStunned", false);
            animator.SetBool("isRunning", false);

            passiveTensionTimer += Time.deltaTime;
            if (passiveTensionTimer >= maxSilentPassiveDuration)
            {
                //TriggerPassiveRelocation();
            }
        }
        else if (currentState == ChaseState)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isStunned", false);
            animator.SetBool("isRunning", true);

            passiveTensionTimer = 0f;
        }
    }

    public void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            if (currentState == RoamState)
            {
                navMeshAgent.speed = MoveSpeed;
            }
            else if (currentState == ChaseState)
            {
                float currentChaseSpeed = baseChaseSpeed * (MoveSpeed / baseRoamSpeed);
                navMeshAgent.speed = currentChaseSpeed;
            }
        }

        if (newState == ChaseState)
        {
            StartChaseAudio();
        }
        else if (currentState == ChaseState && newState != ChaseState)
        {
            StopChaseAudio();
        }

        currentState.EnterState(this);
    }

    public Vector3 GetRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Count == 0) return transform.position;

        int index = Random.Range(0, teleportPoints.Count);
        return teleportPoints[index].position;
    }

    public Vector3 GetRandomPointExcludingPlayerRoom()
    {
        if (teleportPoints == null || teleportPoints.Count == 0) return transform.position;
        if (targetPlayer == null) return GetRandomPoint();

        Vector3 playerPos = targetPlayer.transform.position;
        IRoom playerRoom = RoomUtils.GetRoomForPosition(new Vector2(playerPos.x, playerPos.y));

        List<Transform> validPoints = new List<Transform>();

        foreach (Transform point in teleportPoints)
        {
            if (point == null) continue;

            IRoom pointRoom = RoomUtils.GetRoomForPosition(new Vector2(point.position.x, point.position.y));

            if (playerRoom == null || pointRoom == null || !pointRoom.Equals(playerRoom))
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count > 0)
        {
            int index = Random.Range(0, validPoints.Count);
            return validPoints[index].position;
        }

        return GetRandomPoint();
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
        if (!isEnemyActivated) return;

        StopChaseAudio();

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        StopCoroutine(nameof(UnfreezeAfter));

        isFrozen = true;

        StartCoroutine(UnfreezeAfter(duration));

        if (stunGlitchCoroutine == null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isStunned", true); 
            animator.SetBool("isRunning", false); 
            
            stunGlitchCoroutine = StartCoroutine(StunGlitchLoop());
        }
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
        yield return new WaitForSeconds(seconds);

        if (stunGlitchCoroutine != null)
        {
            StopCoroutine(stunGlitchCoroutine);
            stunGlitchCoroutine = null;
        }

        SetGhostVisuals(true);
        isFrozen = false; 

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = false;
        }

        ChangeState(RoamState);
    }

    private IEnumerator StunGlitchLoop()
    {
        while (true)
        {
            float offDuration = Random.Range(minGlitchInterval, maxGlitchInterval);
            SetGhostVisuals(false);
            yield return new WaitForSeconds(offDuration);

            float onDuration = Random.Range(minGlitchInterval, maxGlitchInterval);
            SetGhostVisuals(true);
            yield return new WaitForSeconds(onDuration);
        }
    }

    private void SetGhostVisuals(bool visible)
    {
        if (enemy == null) return;

        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                r.enabled = visible;
            }
        }
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

        StopChaseAudio();

        // Teleport enemy to a random room that isn't the player's current room,
        // preventing an immediate re-catch after the player respawns.
        TeleportToRandomRoom();

        ChangeState(RoamState);
        StartCoroutine(KillSequence());
    }

    private void TeleportToRandomRoom()
    {
        if (navMeshAgent == null) return;

        Vector3 destination = GetRandomPointExcludingPlayerRoom();

        // Warp moves the agent instantly, bypassing pathfinding so it can't get stuck.
        navMeshAgent.Warp(destination);
        navMeshAgent.ResetPath();

        Debug.Log($"[EnemyStateMachine] PlayerCaught: enemy teleported to {destination}");
    }

    private IEnumerator KillSequence()
    {
        ///Insert Kill Animations and calls here

        yield return new WaitForSeconds(1);
        EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_GAME_RESTART);

        checkpoint.ReturnToCheckpoint();

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

        int tierIndex = Mathf.Clamp(corruptedPaintingsChanneled, 0, stunDurationTiers.Length - 1);
        currentStunDuration = stunDurationTiers[tierIndex];

        //if (corruptedPaintingsChanneled > 2 && !isEnemyActivated)
        //{
        //    isEnemyActivated = true;
        //    AudioSource source = this.gameObject.GetComponent<AudioSource>();
        //    source.Play();
        //    if (navMeshAgent != null)
        //        navMeshAgent.isStopped = false;

        //    ChangeState(RoamState); // only start moving now
        //}

        if (corruptedPaintingsChanneled > 1)
        {
            // method to adjust ghost aggressiveness level based on how many paintings have been restored
            AdjustEnemeyAggressiveness(corruptedPaintingsChanneled);
        }

    }

    //UNTESTED, PLAYTEST FIRST, THIS WILL NEED BALANCING// 
    private void AdjustEnemeyAggressiveness(int corruptedPaintingsChanneled)
    {
        if (corruptedPaintingsChanneled == 1)
        {
            MoveSpeed *= 1.3f;
        }
        else if (corruptedPaintingsChanneled == 3)
        {
            MoveSpeed *= 1.5f;
        }
        else if (corruptedPaintingsChanneled == 4)
        {
            MoveSpeed *= 1.9f;
        }

        UpdateActiveSpeeds();
    }

    private void UpdateActiveSpeeds()
    {
        if (navMeshAgent == null) return;

        if (currentState == RoamState)
        {
            navMeshAgent.speed = MoveSpeed;
        }
        else if (currentState == ChaseState)
        {
            float currentChaseSpeed = baseChaseSpeed * (MoveSpeed / baseRoamSpeed);
            navMeshAgent.speed = currentChaseSpeed;
        }
    }

    public void StartChaseAudio()
    {
        targetPlayer.TryGetComponent<AudioSource>(out AudioSource playerAudioSource);

        if (playerAudioSource != null && !playerAudioSource.isPlaying)
        {
            playerAudioSource.Play();
            Debug.Log("Player Heartbeat SFX Started.");
        }
    }

    public void StopChaseAudio()
    {
        targetPlayer.TryGetComponent<AudioSource>(out AudioSource playerAudioSource);

        if (playerAudioSource != null && playerAudioSource.isPlaying)
        {
            playerAudioSource.Stop();
            Debug.Log("Player Heartbeat SFX Stopped.");
        }
    }

    public EnemySaveData GetSaveData()
    {
        return new EnemySaveData
        {
            position = transform.position,
            isEnemyActivated = this.isEnemyActivated,
            corruptedPaintingsChanneled = this.corruptedPaintingsChanneled
        };
    }

    public void LoadSaveData(EnemySaveData data)
    {
        if (data == null) return;

        this.isEnemyActivated = data.isEnemyActivated;
        this.corruptedPaintingsChanneled = data.corruptedPaintingsChanneled;

        // Safely teleport the NavMeshAgent to the saved location
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.Warp(data.position);
        }
        else
        {
            transform.position = data.position;
        }

        // Restore Stun Tier Limits
        int tierIndex = Mathf.Clamp(corruptedPaintingsChanneled, 0, stunDurationTiers.Length - 1);
        currentStunDuration = stunDurationTiers[tierIndex];

        // Restore Speed/Aggressiveness if applicable
        if (corruptedPaintingsChanneled > 1)
        {
            // Note: Because MoveSpeed is reset to base config in Start(), 
            // calling this here applies the correct multiplier perfectly!
            AdjustEnemeyAggressiveness(corruptedPaintingsChanneled);
        }

        // Resume state based on activation
        if (isEnemyActivated)
        {
            if (navMeshAgent != null) navMeshAgent.isStopped = false;
            ChangeState(RoamState); // Always default to roam on load for fairness
        }
        else
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.ResetPath();
            }
        }
    }

    public void ResetEnemyState()
    {
        isEnemyActivated = false;

        enemy.transform.position = initialEnemyPosition;
        enemy.transform.eulerAngles = initialEnemyRotation;

        if (navMeshAgent != null)
        {
            navMeshAgent.Warp(enemy.transform.position);
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

    }

    public void ReactivateEnemyState()
    {
        isEnemyActivated = true;
        if (navMeshAgent != null)
            navMeshAgent.isStopped = false;
        ChangeState(RoamState);
    }

    public void ReactToLoudNoise(int roomId)
    {
        if (!isEnemyActivated || isFrozen || currentState == ChaseState) return;

        var targetRoom = RoomRegistry.GetRoom(roomId) as RoomComponent;
        if (targetRoom != null)
        {
            // Instead of instant warping, tell the NavMeshAgent to actively patrol to that room
            Vector3 targetPoint = enemyTeleporting.GetForcedTeleportPoint(this, targetRoom);
            if (targetPoint != Vector3.zero)
            {
                NavAgent.SetDestination(targetPoint);
                Debug.Log($"Ghost heard a disturbance in Room {roomId}! Investigating...");
            }
        }
    }
}