using System.Collections.Generic;
using UnityEngine;
using static EventNames;
using UnityEngine.EventSystems;

public class EnemyAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Searching }

    [SerializeField] private float distance = 5f;
    [SerializeField] private float speed = 7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float patrolPauseTime = 0.2f;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float searchFlipInterval = 0.5f;
    [SerializeField] private Collider2D playerDetectorCollider;
    [SerializeField] private float catchDistance = 0.5f;

    private int currentPatrolIndex = 0;

    private float patrolPauseTimer = 0f;

    private bool isPlayerInSight = false;
    private Transform playerTransform;

    private EnemyState currentState = EnemyState.Patrolling;

    [SerializeField] private float searchDuration = 1.5f;
    private float searchTimer = 0f;
    private bool lastSawPlayer = false;
    private bool wasPlayerInSight = false;
    private bool isHidden = false;

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_HID, OnPlayerHiding);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_REVEALED, OnPlayerRevealed);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_HID, OnPlayerHiding);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_REVEALED, OnPlayerRevealed);
    }

    private void OnPlayerHiding()
    {
        isHidden = true;
    }

    private void OnPlayerRevealed()
    {
        isHidden = false;
    }


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

        if (playerNowInSight && !wasPlayerInSight)
        {
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_SPOTTED_PLAYER);
        }
        else if (!playerNowInSight && wasPlayerInSight)
        {
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_LOST_PLAYER);
        }

        wasPlayerInSight = playerNowInSight;

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
                currentState = EnemyState.Searching;
                searchTimer = searchDuration;
                lastSawPlayer = false;
                EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_SEARCHING);
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
                // Only patrol if not searching
                if (currentState == EnemyState.Patrolling)
                    Patrol();
            }
        }

        // --- Position-based player catch check ---
        if (!isHidden && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < catchDistance)
            {
                EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_FAILED);
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
        EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_CHASING);

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
        EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_PATROLLING);

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
            // Start searching instead of pausing
            currentState = EnemyState.Searching;
            searchTimer = searchDuration;
            EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_SEARCHING);
        }
    }

    void Search()
    {
        EventBroadcaster.Instance.PostEvent(EnemyEvents.ENEMY_SEARCHING);

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
