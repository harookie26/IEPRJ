using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static EventNames;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueLineText;

    private Queue<string> dialogueQueue = new();
    private Action onDialogueComplete;

    private HashSet<string> completedDialogues = new();
    private Dictionary<string, int> dialogueIndices = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayNextDialogue(string objectKey, string[] dialogueIDs, Func<string, Dialogue> fetchDialogue, Action onComplete = null)
    {
        if (!dialogueIndices.ContainsKey(objectKey))
            dialogueIndices[objectKey] = 0;

        int index = dialogueIndices[objectKey];

        if (index >= dialogueIDs.Length)
        {
            Debug.Log($"All dialogues for '{objectKey}' have been completed.");
            return;
        }

        string dialogueID = dialogueIDs[index];

        if (HasCompletedDialogue(dialogueID))
        {
            Debug.Log($"Dialogue '{dialogueID}' already completed.");
            return;
        }

        Dialogue dialogue = fetchDialogue(dialogueID);
        if (dialogue == null)
        {
            Debug.LogWarning($"Dialogue '{dialogueID}' not found.");
            return;
        }

        ShowDialogue(dialogue, () =>
        {
            completedDialogues.Add(dialogueID);
            dialogueIndices[objectKey]++;
            onComplete?.Invoke();
        });
    }

    public void ShowDialogue(Dialogue dialogue, Action onComplete = null)
    {
        EventBroadcaster.Instance.PostEvent(UIEvents.PLAY_DIALOGUE_START); 

        characterNameText.gameObject.SetActive(true);
        dialogueLineText.gameObject.SetActive(true);

        characterNameText.text = dialogue.characterName;
        dialogueQueue.Clear();

        foreach (string line in dialogue.dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }

        onDialogueComplete = () =>
        {
            onComplete?.Invoke();
            EventBroadcaster.Instance.PostEvent(UIEvents.PLAY_DIALOGUE_END); 
        };

        ShowNextLine();

    }

    private void ShowNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        string nextLine = dialogueQueue.Dequeue();
        dialogueLineText.text = nextLine;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.StartCoroutine(InputManager.Instance.WaitForInputCoroutine(() =>
            {
                ShowNextLine();
            }));
        }
        else
        {
            Debug.LogWarning("InputManager not found. Skipping input wait.");
            ShowNextLine();
        }
    }

    private void EndDialogue()
    {
        characterNameText.gameObject.SetActive(false);
        dialogueLineText.gameObject.SetActive(false);

        onDialogueComplete?.Invoke();
    }

    public bool HasCompletedDialogue(string dialogueID)
    {
        return completedDialogues.Contains(dialogueID);
    }

}