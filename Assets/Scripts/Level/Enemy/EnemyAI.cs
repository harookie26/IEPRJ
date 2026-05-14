using System.Collections;
using UnityEngine;
using static EventNames;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;

    [Header("Tuning")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float moveDelay = 2f;
    [SerializeField] private float captureCooldown = 1.5f;
    [SerializeField] private float catchDistance = 1.2f;
    [SerializeField] private bool logDebug = false;

    private PlayerLocationUpdater playerLocationUpdater;
    private EnemyStateMachine enemyStateMachine;
    private Coroutine moveToPlayerCoroutine;

    public int enemyLocationID;
    private bool isPlayerInRange = false;
    private bool hasPostedLoseEvent = false;
    private bool canCapture = true;

    private void Start()
    {
        canCapture = true;

        if (player == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                player = playerGo;
        }

        if (player != null)
        {
            playerLocationUpdater = player.GetComponent<PlayerLocationUpdater>();
        }

        enemyStateMachine = GetComponent<EnemyStateMachine>();
        if (enemyStateMachine == null)
            enemyStateMachine = GetComponentInParent<EnemyStateMachine>();
    }

    private void Update()
    {
        if (player == null || playerLocationUpdater == null)
            return;

        int playerRoomId = playerLocationUpdater.getplayerLocationID();

        if (isPlayerInRange)
        {
            CancelTeleport();
            ChasePlayer();
            return;
        }

        if (enemyLocationID != playerRoomId)
        {
            if (moveToPlayerCoroutine == null)
            {
                moveToPlayerCoroutine = StartCoroutine(EnemyMoveTowardsPlayerAfterDelay());
            }
        }
        else
        {
            CancelTeleport();
            ChasePlayer();
        }

        // Debug inputs
        if (Input.GetKeyDown(KeyCode.T)) TeleportToRandomRoom();
        if (Input.GetKeyDown(KeyCode.Y)) TeleportToPlayerRoom();
    }

    private void ChasePlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.transform.position,
            chaseSpeed * Time.deltaTime
        );

        if (canCapture && distanceToPlayer <= catchDistance && !hasPostedLoseEvent)
        {
            hasPostedLoseEvent = true;
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_CATCHED);
            if (logDebug) Debug.Log("[EnemyAI] Physically caught the player!", this);
        }
    }

    private void CancelTeleport()
    {
        if (moveToPlayerCoroutine != null)
        {
            StopCoroutine(moveToPlayerCoroutine);
            moveToPlayerCoroutine = null;
        }
    }

    private IEnumerator EnemyMoveTowardsPlayerAfterDelay()
    {
        yield return new WaitForSeconds(moveDelay);
        yield return StartCoroutine(TeleportSequence());
        moveToPlayerCoroutine = null;
    }

    private IEnumerator TeleportSequence()
    {
        if (enemyStateMachine != null)
        {
            canCapture = false;
            if (logDebug) Debug.Log("[EnemyAI] Teleporting: Capture Disabled.");

            enemyStateMachine.EnemyTeleporting.TeleportToPlayerRoom(enemyStateMachine, playerLocationUpdater);

            yield return new WaitForSeconds(captureCooldown);

            canCapture = true;
            if (logDebug) Debug.Log("[EnemyAI] Cooldown finished: Capture Enabled.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        UpdateRoomID(other);
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerStay(Collider other) => UpdateRoomID(other);

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            hasPostedLoseEvent = false;
        }

        if (other.TryGetComponent<RoomComponent>(out var room) && room.Id == enemyLocationID)
        {
            enemyLocationID = -1;
        }
    }

    private void UpdateRoomID(Collider other)
    {
        if (other != null && other.TryGetComponent<RoomComponent>(out var room))
        {
            enemyLocationID = room.Id;
        }
    }

    private void TeleportToRandomRoom()
    {
        if (enemyStateMachine == null) return;
        StartCoroutine(ManualTeleportRoutine(null));
    }

    private void TeleportToPlayerRoom()
    {
        if (enemyStateMachine == null || playerLocationUpdater == null) return;
        StartCoroutine(ManualTeleportRoutine(playerLocationUpdater));
    }

    private IEnumerator ManualTeleportRoutine(PlayerLocationUpdater target)
    {
        canCapture = false;
        if (target == null)
        {
            enemyStateMachine.EnemyTeleporting.ForcedRoom = null;
            enemyStateMachine.EnemyTeleporting.TeleportNow(enemyStateMachine);
        }
        else
        {
            enemyStateMachine.EnemyTeleporting.TeleportToPlayerRoom(enemyStateMachine, target);
        }
        yield return new WaitForSeconds(captureCooldown);
        canCapture = true;
    }
}