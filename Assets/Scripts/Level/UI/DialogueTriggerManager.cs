using Level.UI;
using UnityEngine;

public class DialogueTriggerManager : MonoBehaviour
{
    public static DialogueTriggerManager Instance { get; private set; }

    [Header("Intro Dialogue")]
    [SerializeField] private DialogueEntry introDialogue1;
    [SerializeField] private DialogueEntry introDialogue2;
    [SerializeField] private DialogueEntry introDialogue3;

    [Header("Locked Door Dialogue")]
    [SerializeField] private DialogueEntry lockedDoorDialogue;

    [Header("Key Found Dialogue")]
    [SerializeField] private DialogueEntry keyFoundDialogue;
    
    [Header("Enemy Intro Dialogue")]
    [SerializeField] private DialogueEntry EnemyIntro1Dialogue;
    [SerializeField] private DialogueEntry EnemyIntro2Dialogue;

    [Header("Paintbucket Dialogue")]
    [SerializeField] private DialogueEntry paintbucketDialogue;

    [Header("Channel Dialogue")]
    [SerializeField] private DialogueEntry channelDialogue;

    [Header("Find Corrupted Dialogue")]
    [SerializeField] private DialogueEntry findCorruptedDialogue1;
    [SerializeField] private DialogueEntry findCorruptedDialogue2;
    [SerializeField] private DialogueEntry findCorruptedDialogue3;
    [SerializeField] private DialogueEntry findCorruptedDialogue4;

    [Header("Painting Backstory Dialogue")]
    [SerializeField] private DialogueEntry paintingBackstoryDialogue;

    [Header("Final Painting Fixed Dialogue")]
    [SerializeField] private DialogueEntry finalPaintingFixedDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            DialogueManager.Instance.Display(introDialogue1);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DialogueManager.Instance.Display(paintingBackstoryDialogue);
        }
    }

    public void TriggerIntroDialogue()
    {
        DialogueManager.Instance.Display(introDialogue1);
        DialogueManager.Instance.Display(introDialogue2);
        DialogueManager.Instance.Display(introDialogue3);
    }

    public void TriggerKeyFoundDialogue()
    {
        DialogueManager.Instance.Display(keyFoundDialogue);
    }

    public void TriggerChannelDialogue()
    {
        DialogueManager.Instance.Display(channelDialogue);
    }

    public void TriggerFindCorruptedDialogue()
    {
        DialogueManager.Instance.Display(findCorruptedDialogue1);
        DialogueManager.Instance.Display(findCorruptedDialogue2);
        DialogueManager.Instance.Display(findCorruptedDialogue3);
        DialogueManager.Instance.Display(findCorruptedDialogue4);
    }

    public void TriggerPaintingBGDialogue(string dialogueID)
    {
        switch (dialogueID)
        {
            case "paint1":
                DialogueManager.Instance.Display(paintingBackstoryDialogue);
                break;
            case "paint2":
                DialogueManager.Instance.Display(paintingBackstoryDialogue);
                break;
            case "paint3":
                DialogueManager.Instance.Display(paintingBackstoryDialogue);
                break;
            case "paint4":
                DialogueManager.Instance.Display(paintingBackstoryDialogue);
                break;
        }
    }

    public void TriggerFinalPaintingFixedDialogue()
    {
        DialogueManager.Instance.Display(finalPaintingFixedDialogue);
    }


    public void TriggerEnemyIntroDialogue()
    {
        DialogueManager.Instance.Display(EnemyIntro1Dialogue);
        DialogueManager.Instance.Display(EnemyIntro2Dialogue);
        DialogueManager.Instance.Display(paintbucketDialogue);
    }

    public void TriggerLockedDoorDialogue()
    {
        DialogueManager.Instance.Display(lockedDoorDialogue);
    }
}