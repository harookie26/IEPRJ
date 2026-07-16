using Unity.VisualScripting;
using UnityEngine;

public class EndingSceneTrigger : MonoBehaviour
{
    [SerializeField] private MainPainting mainPainting;
    [SerializeField] private GameStateManager gameState;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🚨 TRIGGER ENTERED BY: {other.gameObject.name} (Tag: {other.tag})");

        if (other.CompareTag("Player"))
        {

            if (mainPainting != null)
            {
                Debug.Log("PaintingInteractable.Interact: Found MainPainting component. Checking if fully revealed.");
            }

            bool revealedByCovers = mainPainting != null && mainPainting.IsFullyRevealed();

            if (revealedByCovers)
            {
                if (gameState != null)
                {
                    gameState.TriggerWinSequence();
                    Debug.Log("PaintingInteractable.Interact: WIN SEQUENCE TRIGGERED");
                }
                else
                {
                    Debug.LogWarning("PaintingInteractable.Interact: No GameStateManager found to trigger win sequence.");
                }
            }
        }
    }
}