using UnityEngine;

public abstract class EnemyState 
{

    public abstract void EnterState(EnemyStateManager state);

    public abstract void UpdateState(EnemyStateManager state);

    public abstract void OnCollision(EnemyStateManager state);

}
