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

    public void ShowDialogue(string characterName, string dialogueLine, Action onComplete)
    {
        Debug.Log("ShowDialogue called. Setting up dialogue line.");

        if (!dialoguePanel.activeSelf)
        {
            dialoguePanel.SetActive(true);
        }

        characterNameText.text = characterName;
        dialogueLineText.text = dialogueLine;

        // Use the InputManager to wait for input before proceeding.
        if (InputManager.Instance != null)
        {
            InputManager.Instance.StartCoroutine(InputManager.Instance.WaitForInputCoroutine(() =>
            {
                Debug.Log("Input detected. Closing dialogue and completing action.");
                CloseDialoguePanel();
                onComplete?.Invoke();
            }));
        }
        else
        {
            Debug.LogWarning("InputManager instance not found. Cannot wait for input. Completing immediately.");
            onComplete?.Invoke();
        }
    }

    public void CloseDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }
}