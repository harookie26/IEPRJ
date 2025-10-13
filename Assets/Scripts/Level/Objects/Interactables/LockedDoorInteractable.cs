using UnityEngine;
using Game.ObjectTypes;

public class LockedDoorInteractable : MonoBehaviour, IInteractable
{
    private PlayerCollectibleManager collectibles;
    private AudioSource sfxAudioSource;
    private AudioList audioList;

    private void Awake()
    {
        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();

        //Find the AudioList object in the scene
        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject != null)
        {
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }
    }

    public void Interact()
    {
        if (collectibles != null && collectibles.HasCollected("Key"))
        {
            Destroy(gameObject);
            sfxAudioSource.PlayOneShot(audioList.lockedDoorSFX);

        }
        else
        {
            Debug.Log("Door is locked. You need a key to open it.");
            sfxAudioSource.PlayOneShot(audioList.unlockDoorSFX);

        }
    }
}
