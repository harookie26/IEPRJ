using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintText;

    private int corruptedPaintingsChanneled = 0;

    void Start()
    {
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

        // Set initial state[cite: 1]
        OpenHint();
        SetHint1();
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
        corruptedPaintingsChanneled++;
        SetHint4();

        if (corruptedPaintingsChanneled >= 4)
        {
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT5_START);
        }
    }

    private void SetHint1()
    {
        hintText.text = "Find the missing key to the locked door of the museum";
    }

    private void SetHint2()
    {
        hintText.text = "Go to the Main Gallery";
    }

    private void SetHint3()
    {
        hintText.text = "Collect the Paintbucket";
    }

    private void SetHintCorruptedPaintingTutorial()
    {
        hintText.text = "Channel the Corrupted Painting in the Main Gallery";
    }

    private void SetHint4()
    {
        hintText.text = "Find and restore all 4 corrupted paintings and learn its secrets\n" +
            corruptedPaintingsChanneled + "/4 paintings restored";
    }

    private void SetHint5()
    {
        DialogueTriggerManager.Instance.TriggerFinalPaintingFixedDialogue();
        hintText.text = "Go back to the main gallery"; 
    }

    public void OpenHint()
    {
        this.hintPanel.SetActive(true); 
    }

    public void CloseHint()
    {
        this.hintPanel.SetActive(false);
    }
}
