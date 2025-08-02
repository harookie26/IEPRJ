using System.Collections;
using Game.Level;
using UnityEngine;
using static EventNames;

public class EnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        ChasePlayer,
        ChaseObjective,
        FollowPath,
        Dazed
    }

    public State CurrentState { get; private set; } = State.Idle;

    [Header("Idle State Settings")]
    [SerializeField] private float idleDuration = 3f;
    [SerializeField] private float lookInterval = 1f;

    [Header("Patrol State Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private int patrolCyclesPerRoom = 3;
    private int patrolCycleCount = 0;
    private bool reachedTargetRoom = false;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 3f;

    private PathfinderComponent pathfinder;
    private EnemyDetection enemyDetection;
    private Transform playerTransform;
    private Transform objectiveTransform;

    private Coroutine idleCoroutine;
    private bool facingLeft = true;

    private Vector2 patrolTarget;
    private IRoom currentRoom;
    private IRoom initialRoom;

    private bool isPlayerUsingDoor = false;

    [SerializeField] private float roomBoundsOffset = 1.0f;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_STOPPED_USING_DOOR, IsPlayerNotUsingDoor );
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_USING_DOOR, IsPlayerUsingDoor );
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_STOPPED_USING_DOOR, IsPlayerNotUsingDoor );
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_USING_DOOR, IsPlayerUsingDoor );
    }

    private void IsPlayerUsingDoor()
    {
        isPlayerUsingDoor = true;
    }

    private void IsPlayerNotUsingDoor()
    {
        isPlayerUsingDoor = false;
    }

    private void Start()
    {
        if (pathfinder == null)
            pathfinder = GetComponent<PathfinderComponent>();

        if (enemyDetection == null)
            enemyDetection = GetComponent<EnemyDetection>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found in the scene.");
        }

        GameObject objective = GameObject.FindGameObjectWithTag("Objective");
        if (objective != null)
        {
            objectiveTransform = objective.transform;
        }
        else
        {
            Debug.LogWarning("Objective not found in the scene.");
        }

        if (pathfinder != null && pathfinder.TargetRoom == null && pathfinder.targetRoomId != 0)
        {
            initialRoom = RoomRegistry.GetRoom(pathfinder.targetRoomId);
        }
        else
        {
            initialRoom = pathfinder.TargetRoom;
        }

        ChangeState(State.Idle);
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Idle:
                break;

            case State.Patrol:
                HandlePatrol();
                break;

            case State.ChasePlayer:
                HandleChase(playerTransform.position);
                break;

            case State.ChaseObjective:
                HandleChase(objectiveTransform.position);
                break;

            case State.FollowPath:
                HandleFollowPath();
                break;

            case State.Dazed:
                HandleDazed();
                break;
        }
    }

    public void ChangeState(State newState)
    {
        if (CurrentState == State.Idle && idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        CurrentState = newState;

        if (CurrentState == State.Idle)
        {
            idleCoroutine = StartCoroutine(IdleRoutine());
        }
        else if (CurrentState == State.Patrol)
        {
            if (currentRoom == null || !RoomUtils.IsPositionInsideRoom(transform.position, currentRoom))
            {
                currentRoom = RoomUtils.GetRoomForPosition(transform.position);
            }
            patrolTarget = GetRandomPointInRoom(currentRoom);
        }
        else if (CurrentState == State.FollowPath)
        {
            patrolCycleCount = 0;
        }
    }

    IEnumerator IdleRoutine()
    {
        float elapsed = 0f;

        while (elapsed < idleDuration)
        {
            facingLeft = !facingLeft;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facingLeft ? -1 : 1);
            transform.localScale = scale;

            yield return new WaitForSeconds(lookInterval);
            elapsed += lookInterval;
        }

        patrolCycleCount++;

        if (!reachedTargetRoom && patrolCycleCount >= patrolCyclesPerRoom)
        {
            ChangeState(State.FollowPath);
        }
        else
        {
            ChangeState(State.Patrol);
        }
    }

    void HandlePatrol()
    {
        transform.rotation = Quaternion.identity;

        if (currentRoom == null || !RoomUtils.IsPositionInsideRoom(transform.position, currentRoom))
        {
            currentRoom = RoomUtils.GetRoomForPosition(transform.position);
            patrolTarget = GetRandomPointInRoom(currentRoom);
        }

        Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
        if (Vector2.Distance(pos2D, patrolTarget) < 0.1f)
        {
            ChangeState(State.Idle);
        }
        else
        {
            Vector2 direction = (patrolTarget - pos2D).normalized;
            transform.position += (Vector3)(direction * patrolSpeed * Time.deltaTime);
        }
    }

    private Vector2 GetRandomPointInRoom(IRoom room)
    {
        if (room == null) return transform.position;

        // Use OffsetBounds if available
        Bounds b = (room as RoomComponent)?.OffsetBounds ?? room.Bounds;
        float x = Random.Range(b.min.x, b.max.x);
        float y = b.min.y + 1.6f;
        return new Vector2(x, y);
    }

    public void HandleChase(Vector2 target)
    {
        if (playerTransform == null) return;

        if (isPlayerUsingDoor)
        {
            Debug.Log("Player used a door.");
            IRoom playerRoom = RoomUtils.GetRoomForPosition(playerTransform.position);
            if (playerRoom != null && playerRoom != currentRoom)
            {
                if (pathfinder != null)
                {
                    Debug.Log($"[EnemyStateMachine] Player is using a door, changing target room to {playerRoom}");
                    pathfinder.SetTargetRoom(playerRoom);
                    ChangeState(State.FollowPath);
                }
            }
        }
        else
        {

            Vector2 current = new Vector2(transform.position.x, transform.position.y);
            Vector2 direction = (target - current).normalized;
            Vector2 next = current + direction * chaseSpeed * Time.deltaTime;

            transform.position = new Vector3(next.x, next.y, transform.position.z);
        } 
    }
    void HandleFollowPath()
    {
        if (pathfinder != null)
        {
            pathfinder.FollowPath();

            if (currentRoom == null || !RoomUtils.IsPositionInsideRoom(transform.position, currentRoom))
            {
                var newRoom = RoomUtils.GetRoomForPosition(transform.position);
                if (newRoom != null && newRoom != currentRoom)
                {
                    currentRoom = newRoom;
                    ChangeState(State.Idle);
                }
            }
        }
    }

    void HandleDazed()
    {
    }

    public void OnPlayerLost()
    {
        if (pathfinder != null && initialRoom != null)
        {
            Debug.Log("Player lost.");

            pathfinder.SetTargetRoom(initialRoom);
            ChangeState(State.Idle);
        }
    }

    void OnDrawGizmos()
    {
        if (currentRoom != null)
        {
            Gizmos.color = Color.yellow;
            Bounds b = (currentRoom as RoomComponent)?.OffsetBounds ?? currentRoom.Bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}