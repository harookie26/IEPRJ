using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static EventNames;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueLineText;

    [Header("Dialogue Settings")]
    [SerializeField] private float dialogueSpeed = 0.05f;

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

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    public void PlayNextDialogue(string objectKey, string[] dialogueIDs, Func<string, Dialogue> fetchDialogue, Action onComplete = null)
    {
        if (!dialogueIndices.ContainsKey(objectKey))
            dialogueIndices[objectKey] = 0;

        int index = dialogueIndices[objectKey];

        if (index >= dialogueIDs.Length)
            return;

        string dialogueID = dialogueIDs[index];

        if (HasCompletedDialogue(dialogueID))
            return;

        Dialogue dialogue = fetchDialogue(dialogueID);
        if (dialogue == null)
            return;

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

        dialoguePanel.SetActive(true);
        characterNameText.gameObject.SetActive(true);
        dialogueLineText.gameObject.SetActive(true);

        characterNameText.text = dialogue.characterName;
        dialogueQueue.Clear();

        foreach (string line in dialogue.dialogueLines)
            dialogueQueue.Enqueue(line);

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

        StopAllCoroutines();
        StartCoroutine(ShowLineRoutine(nextLine));
    }

    private IEnumerator ShowLineRoutine(string line)
    {
        dialogueLineText.text = "";
        foreach (char c in line)
        {
            dialogueLineText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }

        // Explicitly wait until input is detected
        yield return new WaitUntil(() =>
        {
            // Use InputManager if available, otherwise raw InputSystem
            if (InputManager.Instance != null)
                return InputManager.Instance.WasInteractPressed() || InputManager.Instance.WasAnyKeyExceptChannelPressed();

            return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        });

        ShowNextLine();
    }

    public IEnumerable<string> GetCompletedDialogues()
    {
        return completedDialogues;
    }

    public void RestoreCompletedDialogues(IEnumerable<string> savedSet)
    {
        completedDialogues = new HashSet<string>(savedSet);
        dialogueIndices.Clear(); // optional: reset per-object indices if you want exact rewind
        Debug.Log("[DialogueManager] Restored completed dialogues to checkpoint state.");
    }

    private void EndDialogue()
    {
        characterNameText.gameObject.SetActive(false);
        dialogueLineText.gameObject.SetActive(false);
        dialoguePanel.gameObject.SetActive(false);

        onDialogueComplete?.Invoke();
    }

    public bool HasCompletedDialogue(string dialogueID)
    {
        return completedDialogues.Contains(dialogueID);
    }
}