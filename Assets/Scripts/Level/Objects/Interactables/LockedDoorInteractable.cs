using Game.ObjectTypes;
using UnityEngine;

public class LockedDoorInteractable : MonoBehaviour, IInteractable
{
    public static LockedDoorInteractable Instance { get; private set; }

    [SerializeField] private GameObject doorObject;

    private PlayerCollectibleManager collectibles;
    private AudioSource sfxAudioSource;
    private AudioList audioList;

    public bool hasOpened = false;

    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Optionally uncomment the line below if you want this to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

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

    void Start()
    {
        if (hasOpened)
        {
            doorObject.gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        if (collectibles != null && collectibles.HasCollected("Key"))
        {
            hasOpened = true;
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT2_START);
            sfxAudioSource.PlayOneShot(audioList.lockedDoorSFX);

            doorObject.gameObject.SetActive(false);
            SpatialSFX.Deactivate("door_banging");
        }
        else
        {
            Debug.Log("Door is locked. You need a key to open it.");
            DialogueTriggerManager.Instance.TriggerLockedDoorDialogue();
            sfxAudioSource.PlayOneShot(audioList.unlockDoorSFX);

        }
    }

    public LockedDoorSaveData GetSaveData()
    {
        return new LockedDoorSaveData
        {
            hadDoorOpened = this.hasOpened
        };
    }


    public void LoadSaveData(LockedDoorSaveData data)
    {
        if (data == null) return;
        this.hasOpened = data.hadDoorOpened;

        if (this.hasOpened)
        {
            doorObject.gameObject.SetActive(false);
        }

    }

}
