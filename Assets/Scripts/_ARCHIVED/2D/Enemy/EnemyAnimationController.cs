using UnityEngine;
using static EventNames;

public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;

    void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_PATROLLING, OnEnemyPatrolling);
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_CHASING, OnEnemyChasing);
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_SEARCHING, OnEnemySearching); 
    }

    void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_PATROLLING, OnEnemyPatrolling);
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_CHASING, OnEnemyChasing);
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_SEARCHING, OnEnemySearching);
    }

    void OnEnemyPatrolling()
    {
        if (animator != null)
        {
            animator.SetBool("isPatrolling", true);
            animator.SetBool("isChasing", false);
        }
    }

    void OnEnemyChasing()
    {
        if (animator != null)
        {
            animator.SetBool("isPatrolling", false);
            animator.SetBool("isChasing", true);
        }
    }

    void OnEnemySearching()
    {
        if (animator != null)
        {
            animator.SetBool("isPatrolling", false);
            animator.SetBool("isChasing", false);
        }
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

    }

    void Update()
    {
        
    }
}
