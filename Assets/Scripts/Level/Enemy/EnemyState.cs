using UnityEngine;

public abstract class EnemyState 
{

    public abstract void EnterState(EnemyStateMachine state);

    public abstract void UpdateState(EnemyStateMachine state);

    public abstract void OnCollision(EnemyStateMachine state);

}
