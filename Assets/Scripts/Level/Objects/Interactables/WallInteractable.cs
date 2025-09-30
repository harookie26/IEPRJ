using UnityEngine;
using Game.ObjectTypes;

// A simple interactable that represents a hideable wall.
// If `hideAnchor` is assigned, its position/forward will be provided to the state machine so the player
// can snap/align to that point when hiding. If not assigned, it simply requests hiding without extra data.
[DisallowMultipleComponent]
public class WallInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Optional transform used as the ideal hide point and facing direction. If null the interactor will just request hide without extra data.")]
    public Transform hideAnchor;

    public void Interact()
    {
        var psm = FindFirstObjectByType<PlayerStateMachine>();
        if (psm == null)
        {
            Debug.LogWarning("WallInteractable: No PlayerStateMachine found in scene.");
            return;
        }

        if (hideAnchor != null)
        {
            psm.RequestHide(hideAnchor.position, hideAnchor.forward);
            Debug.Log($"WallInteractable: Requested hide at {hideAnchor.position} (forward {hideAnchor.forward})");
        }
        else
        {
            psm.RequestHide();
            Debug.Log("WallInteractable: Requested hide (no anchor)");
        }
    }
}