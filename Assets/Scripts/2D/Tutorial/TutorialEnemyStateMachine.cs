using System.Collections;
using UnityEngine;

public class TutorialEnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol
    }

    public State CurrentState { get; private set; } = State.Idle;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private Transform patrolPoint;

    [Header("Trigger Settings")]
    [SerializeField] private GameObject triggerObject;

    private bool isPatrolling = false;

    private void Start()
    {
        if (patrolPoint == null)
        {
            Debug.LogError("Patrol Point not set in inspector.");
        }
        if (triggerObject == null)
        {
            Debug.LogError("Trigger Object not set in inspector.");
        }
    }

    private void Update()
    {
        if (!isPatrolling && triggerObject == null)
        {
            StartPatrol();
        }

        if (CurrentState == State.Patrol)
        {
            HandlePatrol();
        }
    }

    private void StartPatrol()
    {
        isPatrolling = true;
        CurrentState = State.Patrol;
    }

    private void HandlePatrol()
    {
        if (patrolPoint == null) return;

        Vector2 pos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 target2D = new Vector2(patrolPoint.position.x, patrolPoint.position.y);

        if (Vector2.Distance(pos2D, target2D) > 0.1f)
        {
            Vector2 direction = (target2D - pos2D).normalized;
            transform.position += (Vector3)(direction * patrolSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (patrolPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(patrolPoint.position, 0.2f);
        }
    }
}