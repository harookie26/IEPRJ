using UnityEngine;
using static EventNames;

public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;

    void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(EnemyEvents.ENEMY_SEARCHING, OnEnemySearching); 
    }

    void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(EnemyEvents.ENEMY_SEARCHING, OnEnemySearching);
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
