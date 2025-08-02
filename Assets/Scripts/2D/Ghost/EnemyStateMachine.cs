using System.Collections;
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        Chase,
        FollowPath,
        Dazed
    }

    public State CurrentState { get; private set; } = State.Idle;

    void Start()
    {
        ChangeState(State.Idle);
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.FollowPath:
                HandleFollowPath();
                break;
            case State.Dazed:
                HandleDazed();
                break;
        }
    }

    public void ChangeState(State newState)
    {
        // Optionally handle exit logic for CurrentState here

        CurrentState = newState;

        // Optionally handle enter logic for newState here
    }

    void HandleIdle()
    {
    }

    void HandlePatrol()
    {
    }

    void HandleChase()
    {
    }

    void HandleFollowPath()
    {
    }

    void HandleDazed()
    {
    }
}