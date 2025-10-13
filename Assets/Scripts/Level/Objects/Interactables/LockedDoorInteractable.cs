using UnityEngine;
using Game.ObjectTypes;

public class LockedDoorInteractable : MonoBehaviour, IInteractable
{
    private PlayerCollectibleManager collectibles;

    private void Awake()
    {
        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();
    }

    public void Interact()
    {
        if (collectibles != null && collectibles.HasCollected("Key"))
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Door is locked. You need a key to open it.");
        }
    }
}
