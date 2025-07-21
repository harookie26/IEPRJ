using System;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
    public GameObject dialoguePanel;

    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueLineText;
    private Action onDialogueLineComplete;
    private bool isDisplayingLine = false;

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

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    void Update()
    {
        // If a dialogue line is being displayed, wait for input to advance.
        if (isDisplayingLine && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            // Mark the current line as complete, allowing the CutsceneManager to proceed.
            isDisplayingLine = false;
            onDialogueLineComplete?.Invoke();
            onDialogueLineComplete = null;
        }
    }

    public void ShowDialogue(string characterName, string dialogueLine, Action onComplete)
    {
        this.onDialogueLineComplete = onComplete;

        // Show the panel if it's not already visible. This handles the start of a conversation.
        if (!dialoguePanel.activeSelf)
        {
            dialoguePanel.SetActive(true);
        }

        // Update the text content.
        characterNameText.text = characterName;
        dialogueLineText.text = dialogueLine;
        
        // The manager is now waiting for input to complete this line.
        isDisplayingLine = true;
    }

    // This new method will be called by a dedicated action to hide the panel.
    public void CloseDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        isDisplayingLine = false;
        onDialogueLineComplete = null; // Clean up any pending callbacks.
    }
}