using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;

    [Header("Tuning")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float moveDelay = 2f;
    [SerializeField] private bool logDebug = false;

    private PlayerLocationUpdater playerLocationUpdater;
    private EnemyStateMachine enemyStateMachine;
    private Coroutine moveToPlayerCoroutine;

    public int enemyLocationID;
    private bool isPlayerInRange = false;   // true while player is inside the enemy's trigger

    private void Start()
    {
        if (player == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                player = playerGo;
        }

        if (player != null)
        {
            playerLocationUpdater = player.GetComponent<PlayerLocationUpdater>();
            if (playerLocationUpdater == null)
                Debug.LogError($"{nameof(EnemyAI)}: `player` has no `{nameof(PlayerLocationUpdater)}` component.", this);
        }
        else
        {
            Debug.LogError($"{nameof(EnemyAI)}: `player` is not assigned and could not be found by tag \"Player\".", this);
        }

        enemyStateMachine = GetComponent<EnemyStateMachine>();
        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponentInParent<EnemyStateMachine>();
        }

        if (enemyStateMachine == null)
        {
            Debug.LogError($"{nameof(EnemyAI)}: EnemyStateMachine not found. Teleporting will be disabled.", this);
        }
        else if (logDebug)
        {
            Debug.Log($"{nameof(EnemyAI)}: EnemyStateMachine initialized for teleporting.", this);
        }
    }

    private void Update()
    {
        if (player == null || playerLocationUpdater == null)
            return;

        int playerRoomId = playerLocationUpdater.getplayerLocationID();

        if (logDebug)
            Debug.Log($"[{nameof(EnemyAI)}] Player room ID={playerRoomId}, Enemy room ID={enemyLocationID}, InRange={isPlayerInRange}", this);

        // Player is nearby just chase, skip room check
        if (isPlayerInRange)
        {
            CancelTeleport();
            ChasePlayer();
            return;
        }

        if (enemyLocationID != playerRoomId)
        {
            // Different room and out of range — teleport after delay
            if (moveToPlayerCoroutine == null)
            {
                if (logDebug) Debug.Log("[EnemyAI] Different room, starting delayed teleport.", this);
                moveToPlayerCoroutine = StartCoroutine(EnemyMoveTowardsPlayerAfterDelay());
            }

        }
        else
        {
            // Same room, out of direct range — still chase normally
            CancelTeleport();
            ChasePlayer();
        }

        // Debug input handling for teleporting
        if (Input.GetKeyDown(KeyCode.T))
        {
            TeleportToRandomRoom();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            TeleportToPlayerRoom();
        }
    }

    private void ChasePlayer()
    {
        if (logDebug) Debug.Log("[EnemyAI] Chasing player.", this);

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.transform.position,
            chaseSpeed * Time.deltaTime
        );
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

        if (enemyStateMachine != null)
        {
            enemyStateMachine.EnemyTeleporting.TeleportToPlayerRoom(enemyStateMachine, playerLocationUpdater);
            if (logDebug) Debug.Log($"[EnemyAI] Teleport to player's room", this);

            // Log where the enemy ended up (will update after trigger enter)
            yield return new WaitForEndOfFrame();
            Debug.Log($"[EnemyAI] Enemy location after teleport: Room ID={enemyLocationID}, Position={transform.position}");
        }
        else
        {
            Debug.LogError("[EnemyAI] Cannot teleport: EnemyStateMachine is null.", this);
        }

        moveToPlayerCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        UpdateRoomID(other);

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (logDebug) Debug.Log("[EnemyAI] Player entered range.", this);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        UpdateRoomID(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (logDebug) Debug.Log("[EnemyAI] Player left range.", this);
        }

        if (other.TryGetComponent<RoomComponent>(out var room) && room.Id == enemyLocationID)
        {
            enemyLocationID = -1;
            if (logDebug) Debug.Log("[EnemyAI] Left room, room ID reset.", this);
        }
    }

    private void UpdateRoomID(Collider other)
    {
        if (other != null && other.TryGetComponent<RoomComponent>(out var room))
        {
            enemyLocationID = room.Id;
            if (logDebug) Debug.Log($"[EnemyAI] In room ID={enemyLocationID}", this);
        }
    }

    private void TeleportToRandomRoom()
    {
        if (enemyStateMachine == null)
        {
            Debug.LogError("[EnemyAI] Cannot teleport: EnemyStateMachine is null.", this);
            return;
        }

        enemyStateMachine.EnemyTeleporting.ForcedRoom = null;
        enemyStateMachine.EnemyTeleporting.TeleportNow(enemyStateMachine);
        if (logDebug) Debug.Log("[EnemyAI] Teleported to random room (T key pressed).", this);
    }

    private void TeleportToPlayerRoom()
    {
        if (enemyStateMachine == null)
        {
            Debug.LogError("[EnemyAI] Cannot teleport: EnemyStateMachine is null.", this);
            return;
        }

        if (playerLocationUpdater == null)
        {
            Debug.LogError("[EnemyAI] Cannot teleport: PlayerLocationUpdater is null.", this);
            return;
        }

        enemyStateMachine.EnemyTeleporting.TeleportToPlayerRoom(enemyStateMachine, playerLocationUpdater);
        if (logDebug) Debug.Log($"[EnemyAI] Teleport to player's room triggered via Y key press.", this);
    }
}