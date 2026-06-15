using Level.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueTriggerManager : MonoBehaviour
{
    public static DialogueTriggerManager Instance { get; private set; }

    [Header("Intro Dialogue")]
    [SerializeField] private List<DialogueEntry> introDialogues;

    [Header("Locked Door Dialogue")]
    [SerializeField] private List<DialogueEntry> lockedDoorDialogues;

    [Header("Key Found Dialogue")]
    [SerializeField] private DialogueEntry keyFoundDialogue;

    [Header("Enemy Intro Dialogue")]
    [SerializeField] private List<DialogueEntry> EnemyIntroDialogue;

    [Header("Find Flashlight Dialogue")]
    [SerializeField] private DialogueEntry findFlashlightDialogue;

    [Header("Flashlight Dialogue")]
    [SerializeField] private List<DialogueEntry> flashlightDialogues;

    [Header("Paintbucket Dialogue")]
    [SerializeField] private DialogueEntry paintbucketDialogue;

    [Header("Channel Dialogue")]
    [SerializeField] private DialogueEntry channelDialogue;

    [Header("Find Corrupted Dialogue")]
    [SerializeField] private DialogueEntry findCorruptedDialogue1;
    [SerializeField] private DialogueEntry findCorruptedDialogue2;

    [Header("Painting Backstory Dialogue")]
    [SerializeField] private List<DialogueEntry> paintingBackstoryDialogues;

    [Header("Final Painting Fixed Dialogue")]
    [SerializeField] private DialogueEntry finalPaintingFixedDialogue;

    private bool lockedDoorDialogueCooldown = false;

    private List<int> triggeredDialogueIDs;

    private bool ishintOpen = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (triggeredDialogueIDs == null)
        {
            triggeredDialogueIDs = new List<int>();
        }
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.T))
        {
            //DialogueManager.Instance.Display(introDialogue1);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DialogueManager.Instance.Display(paintingBackstoryDialogue1);
        }*/
    }

    public void TriggerIntroDialogue()
    {
        Debug.Log($"[Dialogue] TriggerIntroDialogue called! List size is currently: {triggeredDialogueIDs.Count}");

        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(1) || triggeredDialogueIDs.Contains(2) || triggeredDialogueIDs.Contains(3))
        {
            HintManager.Instance.OpenHint();
            return;
        }
        else
        {
            triggeredDialogueIDs.Add(1);
            triggeredDialogueIDs.Add(2);
            triggeredDialogueIDs.Add(3);

            DialogueManager.Instance.DisplaySequence(introDialogues);

            float totalDuration = introDialogues[0].displayDuration + introDialogues[1].displayDuration + introDialogues[2].displayDuration;
        }
    }

    private IEnumerator OpenHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HintManager.Instance.OpenHint();
    }

    public void TriggerKeyFoundDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(4)) return;

        triggeredDialogueIDs.Add(4);
        DialogueManager.Instance.DisplaySequence(new[] { keyFoundDialogue });
    }

    public void TriggerFindFlashlightFirstDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        DialogueManager.Instance.DisplaySequence(new[] { findFlashlightDialogue });
    }

    public void TriggerChannelDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(5)) return;
        triggeredDialogueIDs.Add(5);

        DialogueManager.Instance.DisplaySequence(new[] { channelDialogue });
    }

    public void TriggerFindCorruptedDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(6) || triggeredDialogueIDs.Contains(7))
        {
            return;
        }
        triggeredDialogueIDs.Add(6);
        triggeredDialogueIDs.Add(7);

        DialogueManager.Instance.DisplaySequence(new[] { findCorruptedDialogue1, findCorruptedDialogue2 });
    }

    public DialoguePlaybackHandle TriggerPaintingBGDialogue(string dialogueID)
    {
        if (SaveCourier.IsLoadingSave) return null;

        switch (dialogueID)
        {
            case "paint1":
                if (triggeredDialogueIDs.Contains(8)) return null;
                triggeredDialogueIDs.Add(8);
                return DialogueManager.Instance.DisplaySequence(new[] { paintingBackstoryDialogues[0] });
            case "paint2":
                if (triggeredDialogueIDs.Contains(9)) return null;
                triggeredDialogueIDs.Add(9);
                return DialogueManager.Instance.DisplaySequence(new[]
                {
                    paintingBackstoryDialogues[1],
                    paintingBackstoryDialogues[2]
                });
            case "paint3":
                if (triggeredDialogueIDs.Contains(10)) return null;
                triggeredDialogueIDs.Add(10);
                return DialogueManager.Instance.DisplaySequence(new[] { paintingBackstoryDialogues[3] });
            case "paint4":
                if (triggeredDialogueIDs.Contains(11)) return null;
                triggeredDialogueIDs.Add(11);
                return DialogueManager.Instance.DisplaySequence(new[] { paintingBackstoryDialogues[4] });
        }

        return null;
    }

    public void TriggerFinalPaintingFixedDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(12)) return;
        triggeredDialogueIDs.Add(12);
        DialogueManager.Instance.DisplaySequence(new[] { finalPaintingFixedDialogue });
    }


    public void TriggerEnemyIntroDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(13) || triggeredDialogueIDs.Contains(14) || triggeredDialogueIDs.Contains(15))
        {
            return;
        }
        else
        {
            triggeredDialogueIDs.Add(13);
            triggeredDialogueIDs.Add(14);
            triggeredDialogueIDs.Add(15);
            DialogueManager.Instance.DisplaySequence(EnemyIntroDialogue);
        }

        if (PlayerCollectibleManager.Instance.HasCollected("Flashlight"))
        {
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT3_START);
            DialogueTriggerManager.Instance.TriggerPaintbucketDialogue();
        }
        else
        {
            HintManager.Instance.SetHintFindFlashlight();
        }

    }

    public void TriggerPaintbucketDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(16))
        {
            return;
        }
        else
        {
            triggeredDialogueIDs.Add(16);
            DialogueManager.Instance.DisplaySequence(new[] { paintbucketDialogue });
        }

    }

    public void TriggerFlashlightDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (triggeredDialogueIDs.Contains(17))
        {
            return;
        }
        else
        {
            triggeredDialogueIDs.Add(17);
            DialogueManager.Instance.DisplaySequence(flashlightDialogues);
        }

    }

    public void TriggerLockedDoorDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (lockedDoorDialogueCooldown) return;

        DialogueManager.Instance.DisplaySequence(lockedDoorDialogues);

        StartCoroutine(LockedDoorDialogueTimer());

        if (ishintOpen == false)
        {
            StartCoroutine(OpenHintAfterDelay(20));
            ishintOpen = true;
        }
    }

    private IEnumerator LockedDoorDialogueTimer()
    {
        lockedDoorDialogueCooldown = true;
        yield return new WaitForSeconds(5f);
        lockedDoorDialogueCooldown = false;
    }

    public void TriggerInaccessibleAreaDialogue()
    {
        if (SaveCourier.IsLoadingSave) return;

        DialogueManager.Instance.DisplayLatest("Georgie", "I think we should explore this area later."
            , 0.25f, 3f, 0.25f);
    }

    public DialogueSaveData GetSaveData()
    {
        return new DialogueSaveData
        {
            // This 'new List' part is crucial. It takes a snapshot of your list 
            // at the exact moment of saving, rather than passing a live connection.
            triggeredDialogueIds = new List<int>(this.triggeredDialogueIDs)
        };
    }

    public void LoadSaveData(DialogueSaveData data)
    {
        Debug.Log($"[Save System] Dialogue load triggered. Is data null? {data == null}");

        if (data == null)
        {
            // If data is null, we still need to make sure the list isn't null!
            triggeredDialogueIDs = new List<int>();
            return;
        }

        if (data.triggeredDialogueIds != null)
        {
            triggeredDialogueIDs = new List<int>(data.triggeredDialogueIds);
            Debug.Log($"[Save System] Loaded {triggeredDialogueIDs.Count} triggered dialogues.");
        }
        else
        {
            triggeredDialogueIDs = new List<int>();
            Debug.Log("[Save System] No dialogues found in save, starting fresh.");
        }
    }
}
