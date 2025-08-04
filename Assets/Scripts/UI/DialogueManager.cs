using System;
using UnityEngine;
using TMPro;
using static EventNames;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Components")]
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
    }

    public void ShowDialogue(string characterName, string dialogueLine, Action onComplete)
    {
        Debug.Log("ShowDialogue called. Setting up dialogue line.");

        characterNameText.gameObject.SetActive(true);
        dialogueLineText.gameObject.SetActive(true);

        characterNameText.text = characterName;
        dialogueLineText.text = dialogueLine;

        // Disable all input except LMB/Enter
        EventBroadcaster.Instance.PostEvent(ControlEvents2D.ON_2D_PLAYERCONTROLS_DISABLED);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.StartCoroutine(InputManager.Instance.WaitForInputCoroutine(() =>
            {
                Debug.Log("Input detected. Closing dialogue and completing action.");

                characterNameText.gameObject.SetActive(false);
                dialogueLineText.gameObject.SetActive(false);

                // Re-enable all input
                EventBroadcaster.Instance.PostEvent(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED);

                onComplete?.Invoke();
            }));
        }
        else
        {
            Debug.LogWarning("InputManager instance not found. Cannot wait for input. Completing immediately.");
            // Re-enable all input just in case
            EventBroadcaster.Instance.PostEvent(ControlEvents2D.ON_2D_PLAYERCONTROLS_ENABLED);
            onComplete?.Invoke();
        }
    }
}