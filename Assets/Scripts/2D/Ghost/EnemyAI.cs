using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Searching }

    [SerializeField] private float distance = 5f;
    [SerializeField] private float speed = 7f; // Movement speed
    [SerializeField] private float edgeCheckDistance = 0.1f; // How far ahead to check for ground
    [SerializeField] private LayerMask groundLayer; // Assign this to "Ground" in the Inspector
    [SerializeField] private float patrolPauseTime = 0.2f; // Time to pause after turning
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float searchFlipInterval = 0.5f; // Time between left/right looks

    private int currentPatrolIndex = 0;

    private float patrolPauseTimer = 0f;

    private bool isPlayerInSight = false;
    private Transform playerTransform;

    private EnemyState currentState = EnemyState.Patrolling;

    [SerializeField] private float searchDuration = 1.5f; // How long to look left/right
    private float searchTimer = 0f;
    private bool lastSawPlayer = false;

    void Start()
    {
        isPlayerInSight = false;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        bool playerNowInSight = CheckPlayer();

        if (playerNowInSight)
        {
            currentState = EnemyState.Chasing;
            lastSawPlayer = true;
            ChasePlayer();
        }
        else
        {
            if (currentState == EnemyState.Chasing && lastSawPlayer)
            {
                // Just lost sight of player, start searching
                currentState = EnemyState.Searching;
                searchTimer = searchDuration;
                lastSawPlayer = false;
            }

            if (currentState == EnemyState.Searching)
            {
                Search();
            }
            else if (patrolPauseTimer > 0f)
            {
                patrolPauseTimer -= Time.deltaTime;
            }
            else
            {
                currentState = EnemyState.Patrolling;
                Patrol();
            }
        }
    }


    public bool CheckPlayer()
    {
        Hiding_Script hiding_Script = Object.FindFirstObjectByType<Hiding_Script>();

        Vector2 direction = transform.right * Mathf.Sign(transform.localScale.x);

        int playerLayer = LayerMask.GetMask("Player");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, playerLayer);

        if (hit.collider != null && hit.collider.CompareTag("Player") && (hiding_Script.isHiding == false))
        {
            Debug.Log("Player is in front of the enemy!");
            isPlayerInSight = true;
        }
        else
        {
            isPlayerInSight = false;
        }

        Debug.DrawRay(transform.position, direction * distance, Color.red);

        return isPlayerInSight;
    }

    public void ChasePlayer()
    {
        if (playerTransform == null) return;

        // Flip the enemy to face the player
        float direction = playerTransform.position.x - transform.position.x;
        if (direction != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            transform.localScale = scale;
        }

        // Move towards the player's position
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTransform.position,
            speed * Time.deltaTime
        );
    }
    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector2 targetPosition = new Vector2(targetPoint.position.x, transform.position.y);

        // Move towards the current patrol point
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Flip sprite to face direction
        float direction = targetPoint.position.x - transform.position.x;
        if (direction != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            transform.localScale = scale;
        }

        // Check if reached the patrol point
        if (Mathf.Abs(transform.position.x - targetPoint.position.x) < 0.05f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            patrolPauseTimer = patrolPauseTime; // Optional: pause at each point
        }
    }
    void Search()
    {
        int flipCount = Mathf.FloorToInt((searchDuration - searchTimer) / searchFlipInterval);

        if (flipCount % 2 == 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            currentState = EnemyState.Patrolling;
            patrolPauseTimer = patrolPauseTime; // Optional: pause before resuming patrol
        }
    }


}
