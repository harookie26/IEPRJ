using Game.ObjectTypes;
using System.Collections;
using UnityEngine;

public class LockedDoorInteractable : MonoBehaviour, IInteractable
{
    public static LockedDoorInteractable Instance { get; private set; }

    public bool useRotatingDoorUnlock = false;

    [SerializeField] private GameObject doorObject;
    [SerializeField] private Transform entranceDoor002;
    [SerializeField] private Transform entranceDoor003;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Vector3 entranceDoor002OpenEulerOffset = new Vector3(0f, 0f, -90f);
    [SerializeField] private Vector3 entranceDoor003OpenEulerOffset = new Vector3(0f, 0f, 90f);
    [SerializeField, Min(0.01f)] private float doorOpenDuration = 1f;

    private PlayerCollectibleManager collectibles;
    private AudioSource sfxAudioSource;
    private AudioList audioList;
    private Quaternion entranceDoor002ClosedRotation;
    private Quaternion entranceDoor003ClosedRotation;
    private Coroutine doorOpenCoroutine;

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
        ResolveDoorReferences();
        CacheClosedDoorRotations();

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
            ApplyUnlockedState(false);
        }
    }

    public void Interact()
    {
        if (collectibles != null && collectibles.HasCollected("Key"))
        {
            hasOpened = true;
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT2_START);
            if (sfxAudioSource != null && audioList != null && audioList.unlockDoorSFX != null)
            {
                sfxAudioSource.PlayOneShot(audioList.unlockDoorSFX);
            }

            ApplyUnlockedState(true);
            SpatialSFX.Deactivate("door_banging");
        }
        else
        {
            Debug.Log("Door is locked. You need a key to open it.");
            DialogueTriggerManager.Instance.TriggerLockedDoorDialogue();
            if (sfxAudioSource != null && audioList != null && audioList.lockedDoorSFX != null)
            {
                sfxAudioSource.PlayOneShot(audioList.lockedDoorSFX);
            }

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
            ApplyUnlockedState(false);
        }

    }

    private void ApplyUnlockedState(bool animate)
    {
        if (!useRotatingDoorUnlock)
        {
            if (doorObject != null)
            {
                doorObject.SetActive(false);
            }

            return;
        }

        if (doorObject != null)
        {
            doorObject.SetActive(true);
        }

        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }

        if (doorOpenCoroutine != null)
        {
            StopCoroutine(doorOpenCoroutine);
            doorOpenCoroutine = null;
        }

        if (animate && isActiveAndEnabled)
        {
            doorOpenCoroutine = StartCoroutine(AnimateDoorsOpen());
        }
        else
        {
            SetDoorOpenRotations();
        }
    }

    private IEnumerator AnimateDoorsOpen()
    {
        Quaternion door002StartRotation = entranceDoor002 != null
            ? entranceDoor002.localRotation
            : Quaternion.identity;
        Quaternion door003StartRotation = entranceDoor003 != null
            ? entranceDoor003.localRotation
            : Quaternion.identity;
        Quaternion door002TargetRotation = entranceDoor002ClosedRotation * Quaternion.Euler(entranceDoor002OpenEulerOffset);
        Quaternion door003TargetRotation = entranceDoor003ClosedRotation * Quaternion.Euler(entranceDoor003OpenEulerOffset);

        float elapsed = 0f;
        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            t = t * t * (3f - 2f * t);

            if (entranceDoor002 != null)
            {
                entranceDoor002.localRotation = Quaternion.Lerp(door002StartRotation, door002TargetRotation, t);
            }

            if (entranceDoor003 != null)
            {
                entranceDoor003.localRotation = Quaternion.Lerp(door003StartRotation, door003TargetRotation, t);
            }

            yield return null;
        }

        SetDoorOpenRotations();
        doorOpenCoroutine = null;
    }

    private void SetDoorOpenRotations()
    {
        if (entranceDoor002 != null)
        {
            entranceDoor002.localRotation = entranceDoor002ClosedRotation * Quaternion.Euler(entranceDoor002OpenEulerOffset);
        }

        if (entranceDoor003 != null)
        {
            entranceDoor003.localRotation = entranceDoor003ClosedRotation * Quaternion.Euler(entranceDoor003OpenEulerOffset);
        }
    }

    private void ResolveDoorReferences()
    {
        if (doorObject != null)
        {
            if (entranceDoor002 == null)
            {
                entranceDoor002 = FindChildByName(doorObject.transform, "Entrance_Door_002");
            }

            if (entranceDoor003 == null)
            {
                entranceDoor003 = FindChildByName(doorObject.transform, "Entrance_Door_003");
            }
        }

        if (blockingCollider == null)
        {
            GameObject blockingColliderObject = GameObject.Find("BlockingCollider");
            if (blockingColliderObject != null)
            {
                blockingCollider = blockingColliderObject.GetComponent<Collider>();
            }
        }
    }

    private void CacheClosedDoorRotations()
    {
        if (entranceDoor002 != null)
        {
            entranceDoor002ClosedRotation = entranceDoor002.localRotation;
        }

        if (entranceDoor003 != null)
        {
            entranceDoor003ClosedRotation = entranceDoor003.localRotation;
        }
    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

}
