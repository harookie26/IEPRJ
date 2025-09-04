using System.Collections;
using Game.Level;
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        ChasePlayer,
        ChaseObjective,
        FollowPath,
        Dazed,
        ShoutDistracted
    }

    public State CurrentState { get; private set; } = State.Idle;

    [Header("Idle State Settings")]
    [SerializeField] private float idleDuration = 3f;
    [SerializeField] private float lookInterval = 1f;

    [Header("Patrol State Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private int patrolCyclesPerRoom = 3;
    private int patrolCycleCount = 0;
    private readonly bool reachedTargetRoom = false;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Shout Distracted Settings")]
    [SerializeField] private Vector2 shoutDistractedOffset = new(1.0f, 1.0f);

    private PathfinderComponent pathfinder;
    private EnemyDetection enemyDetection;
    private Transform playerTransform;
    private Transform objectiveTransform;

    private Coroutine idleCoroutine;
    private bool facingLeft = true;

    private Vector2 patrolTarget;
    private IRoom currentRoom;
    private IRoom initialRoom;

    private bool shoutDistractedHandled = false;

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

            case State.ShoutDistracted:
                if (!shoutDistractedHandled)
                {
                    HandleShoutDistracted();
                    shoutDistractedHandled = true;
                }
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

        // Reset the flag when entering ShoutDistracted
        if (CurrentState == State.ShoutDistracted)
        {
            shoutDistractedHandled = false;
        }

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

        Vector2 current = new Vector2(transform.position.x, transform.position.y);
        Vector2 direction = (target - current).normalized;
        Vector2 next = current + direction * chaseSpeed * Time.deltaTime;

        transform.position = new Vector3(next.x, next.y, transform.position.z);

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

    public void HandleShoutDistracted()
    {
        if (playerTransform != null)
        {
            Vector2 targetPosition = (Vector2)playerTransform.position + shoutDistractedOffset;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);

            // Update the pathfinder's current room after moving
            if (pathfinder != null)
            {
                pathfinder.CurrentRoom = RoomUtils.GetRoomForPosition(transform.position);
                pathfinder.SetTargetRoom(initialRoom);
            }
        }

        ChangeState(State.Idle);
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