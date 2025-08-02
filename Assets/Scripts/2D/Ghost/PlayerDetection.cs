using System.Collections;
using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private LayerMask detectionLayerMask;

    private EnemyStateMachine enemyStateMachine;
    private Transform playerTransform;

    void Start()
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

        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponent<EnemyStateMachine>();
        }
    }

    void Update()
    {
        if (playerTransform == null || enemyStateMachine == null)
            return;

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionDistance)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionDistance, detectionLayerMask);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                enemyStateMachine.ChangeState(EnemyStateMachine.State.Chase);
            }
        }
    }
}