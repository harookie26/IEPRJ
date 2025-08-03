using System.Collections;
using UnityEngine;
using static EventNames;
using static EnemyStateMachine;

public class EnemyDetection : MonoBehaviour
{
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private LayerMask detectionLayerMask;

    private EnemyStateMachine enemyStateMachine;
    private Transform playerTransform;
    private Transform objectiveTransform;

    [HideInInspector] public bool isPlayerDetected;
    [HideInInspector] public bool isObjectiveDetected;

    private bool wasPlayerDetectedLastFrame = false;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_HID, OnPlayerHid);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_HID, OnPlayerHid);
    }

    private void OnPlayerHid()
    {
        if (enemyStateMachine != null)
        {
            enemyStateMachine.ChangeState(State.Idle);
        }
    }

    private void Start()
    {
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

        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponent<EnemyStateMachine>();
        }
    }

    private bool IsPlayerHiding()
    {
        // Example: Replace with your actual hiding check logic
        var playerState = playerTransform?.GetComponent<PlayerStateMachine>();
        return playerState != null && playerState.IsHiding;
    }

    private void Update()
    {
        if (playerTransform == null || objectiveTransform == null || enemyStateMachine == null)
            return;

        // Prevent detection if player is hiding
        if (IsPlayerHiding())
        {
            isPlayerDetected = false;
            if (wasPlayerDetectedLastFrame)
            {
                enemyStateMachine.OnPlayerLost();
                wasPlayerDetectedLastFrame = false;
            }
            return;
        }

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        Debug.DrawRay(transform.position, directionToPlayer * detectionDistance, Color.red);

        bool playerDetectedThisFrame = false;
        if (distanceToPlayer <= detectionDistance)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionDistance, detectionLayerMask);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                playerDetectedThisFrame = true;
                enemyStateMachine.ChangeState(State.ChasePlayer);
            }
        }
        isPlayerDetected = playerDetectedThisFrame;

        Vector2 directionToObjective = (objectiveTransform.position - transform.position).normalized;
        float distanceToObjective = Vector2.Distance(transform.position, objectiveTransform.position);

        Debug.DrawRay(transform.position, directionToObjective * detectionDistance, Color.blue);

        if (distanceToObjective <= detectionDistance)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToObjective, detectionDistance, detectionLayerMask);
            if (hit.collider != null && hit.collider.CompareTag("Objective"))
                enemyStateMachine.ChangeState(State.ChaseObjective);
        }

        if (wasPlayerDetectedLastFrame && !isPlayerDetected)
        {
            enemyStateMachine.OnPlayerLost();
        }

        wasPlayerDetectedLastFrame = isPlayerDetected;
    }
}