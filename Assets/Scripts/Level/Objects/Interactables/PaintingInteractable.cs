using UnityEngine;
using Game.ObjectTypes;

[RequireComponent(typeof(Collider))]
public class PaintingInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // Find the enemy state machine in the scene and tell it to distract at this painting.
        var enemyStateMachine = Object.FindFirstObjectByType<EnemyStateMachine>();
        if (enemyStateMachine == null)
        {
            Debug.LogWarning("PaintingInteractable.Interact: No EnemyStateMachine found in scene.");
            return;
        }

        enemyStateMachine.DistractAt(transform.position);
    }
}