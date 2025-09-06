using System.Collections;
using UnityEngine;
using System.Linq;
using static EventNames;
using static EnemyStateMachine;

public class EnemyDetection : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private LayerMask playerLayer;

    private Transform player;
    private bool isChasing = false;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isChasing && distance <= detectionRadius && HasLineOfSight())
        {
            isChasing = true;
        }

        if (isChasing)
        {
            ChasePlayer();
        }
    }

    bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (player.position - origin).normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRadius))
        {
            Debug.DrawRay(origin, direction * detectionRadius, Color.red);
            return hit.transform == player;
        }

        return false;
    }


    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * chaseSpeed * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}