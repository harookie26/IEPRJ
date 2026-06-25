using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintText;

    public int corruptedPaintingsChanneled = 0;

    private List<int> triggeredHintIDs;
    private AudioList audioList;
    private AudioSource sfxAudioSource;

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
            return;
        }

        audioList = FindAnyObjectByType<AudioList>();
        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");
        if (audioObject != null)
        {
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        if (triggeredHintIDs == null)
        {
            triggeredHintIDs = new List<int>();
        }

        // Subscribe to the hint events defined in EventNames[cite: 2, 3]
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT1_START, SetHint1);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT2_START, SetHint2);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT3_START, SetHint3);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT_PAINTING_START, SetHintCorruptedPaintingTutorial);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT4_START, SetHint4);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.HINT5_START, SetHint5);

        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.ADD_PAINTING_RESTORED, AddRestoredPainting);

        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.OPEN_HINT, OpenHint);
        EventBroadcaster.Instance.AddObserver(EventNames.HintEvents.CLOSE_HINT, CloseHint);

        StartCoroutine(StartHintDisplay());
    }

    private IEnumerator StartHintDisplay()
    {
        yield return new WaitForSeconds(0.25f);
        if (triggeredHintIDs.Count == 0)
        {
            SetHint1();
        }
        else
        {
            UpdateDisplayedHint();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks or null reference exceptions
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT1_START, SetHint1);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT2_START, SetHint2);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT3_START, SetHint3);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT_PAINTING_START, SetHintCorruptedPaintingTutorial);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT4_START, SetHint4);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.HINT5_START, SetHint5);

        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.ADD_PAINTING_RESTORED, AddRestoredPainting);

        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.OPEN_HINT, OpenHint);
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.HintEvents.CLOSE_HINT, CloseHint);
    }


    private void AddRestoredPainting()
    {
        OpenHint();
        corruptedPaintingsChanneled++;
        SetHint4();
        PlayObjectiveProgressSfx();

        if (corruptedPaintingsChanneled >= 4)
        {
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT5_START);
        }
    }

    private void SetHint1()
    {
        triggeredHintIDs.Add(1);
        hintText.text = "Find the missing key to the locked door of the museum";
    }

    private void SetHint2()
    {
        OpenHint();
        bool isNewObjective = !triggeredHintIDs.Contains(2);
        if (isNewObjective)
        {
            triggeredHintIDs.Add(2);
        }

        hintText.text = "Go to the Main Gallery";
        if (isNewObjective) PlayObjectiveProgressSfx();
    }

    public void SetHintFindFlashlight()
    {
        OpenHint();
        if (!PlayerCollectibleManager.Instance.HasCollected("Flashlight"))
        {
            bool isNewObjective = !triggeredHintIDs.Contains(7);
            if (isNewObjective)
            {
                triggeredHintIDs.Add(7);
            }

            hintText.text = "Find a flashlight somewhere in the other rooms";
            if (isNewObjective) PlayObjectiveProgressSfx();
        }
    }

    public void SetHint3()
    {
        OpenHint();
        bool isNewObjective = !triggeredHintIDs.Contains(3);
        if (isNewObjective)
        {
            triggeredHintIDs.Add(3);
        }

        hintText.text = "Collect the Paintbucket";
        if (isNewObjective) PlayObjectiveProgressSfx();
    }

    public void SetHintCorruptedPaintingTutorial()
    {
        OpenHint();
        bool isNewObjective = !triggeredHintIDs.Contains(4);
        if (isNewObjective)
        {
            triggeredHintIDs.Add(4);
        }

        hintText.text = "Channel the <b><color=#ff4444>Corrupted Painting</color></b> in the Main Gallery\nHold <b><color=#FFD700>[E]</color></b> to channel";
        if (isNewObjective) PlayObjectiveProgressSfx();
    }

    private void SetHint4()
    {
        OpenHint();
        bool isNewObjective = !triggeredHintIDs.Contains(5);
        if (isNewObjective)
        {
            triggeredHintIDs.Add(5);
        }

        if (corruptedPaintingsChanneled < 4)
        {
            hintText.text = "Find and restore all 4 corrupted paintings and learn its secrets\n" +
            corruptedPaintingsChanneled + "/4 paintings restored";
        }

        if (isNewObjective && corruptedPaintingsChanneled == 0)
        {
            PlayObjectiveProgressSfx();
        }
    }

    private void SetHint5()
    {
        OpenHint();
        if (!triggeredHintIDs.Contains(6))
        {
            triggeredHintIDs.Add(6);
        }

        DialogueTriggerManager.Instance.TriggerFinalPaintingFixedDialogue();
        hintText.text = "Go back to the main gallery";
    }

    private void PlayObjectiveProgressSfx()
    {
        if (StairsInputManager.IsTransferInProgress)
        {
            return;
        }

        if (audioList == null)
        {
            audioList = FindAnyObjectByType<AudioList>();
        }

        if (sfxAudioSource == null)
        {
            GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");
            if (audioObject != null)
            {
                sfxAudioSource = audioObject.GetComponent<AudioSource>();
            }
        }

        if (sfxAudioSource != null && audioList != null && audioList.objectivesSFX != null)
        {
            sfxAudioSource.PlayOneShot(audioList.objectivesSFX);
        }
    }

    public void OpenHint()
    {
        this.hintPanel.SetActive(true);
    }

    public void CloseHint()
    {
        this.hintPanel.SetActive(false);
    }

    public void UpdateDisplayedHint()
    {
        if (triggeredHintIDs == null || triggeredHintIDs.Count == 0) return;

        // Look at the LAST hint added to the list to figure out what text to show
        int currentHint = triggeredHintIDs[triggeredHintIDs.Count - 1];

        switch (currentHint)
        {
            case 1:
                hintText.text = "Find the missing key to the locked door of the museum";
                break;
            case 2:
                hintText.text = "Go to the Main Gallery";
                break;
            case 3:
                hintText.text = "Collect the Paintbucket";
                break;
            case 4:
                hintText.text = "Channel the <b><color=#ff4444>Corrupted Painting</color></b> in the Main Gallery\nHold <b><color=#FFD700>[E]</color></b> to channel";
                break;
            case 5:
                hintText.text = "Find and restore all 4 corrupted paintings and learn its secrets\n" +
                                corruptedPaintingsChanneled + "/4 paintings restored";
                break;
            case 6:
                hintText.text = "Go back to the main gallery";
                break;
            case 7:
                hintText.text = "Find a flashlight somewhere in the other rooms";
                break;
        }
    }

    public HintSaveData GetSaveData()
    {
        return new HintSaveData
        {
            triggeredHintIds = new List<int>(this.triggeredHintIDs),
            // YOU MUST ADD THIS TO YOUR HintSaveData CLASS!
            savedCorruptedPaintingsChanneled = this.corruptedPaintingsChanneled
        };
    }

    public void LoadSaveData(HintSaveData data)
    {
        if (data == null)
        {
            triggeredHintIDs = new List<int>();
            corruptedPaintingsChanneled = 0;
            return;
        }

        if (data.triggeredHintIds != null)
        {
            triggeredHintIDs = new List<int>(data.triggeredHintIds);
        }
        else
        {
            triggeredHintIDs = new List<int>();
        }

        // Load the painting progress!
        corruptedPaintingsChanneled = data.savedCorruptedPaintingsChanneled;

        // Now it's safe to update the UI
        UpdateDisplayedHint();
    }
}
