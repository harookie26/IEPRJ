using System.Collections.Generic;
using UnityEngine;

public class DialogueRegistryManager : MonoBehaviour
{
    public static DialogueRegistryManager Instance { get; private set; }

    private Dictionary<string, Dialogue> dialogueLookup = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            RegisterAllDialogues();
        }
    }

    private void RegisterAllDialogues()
    {
        Register(DialogueMasterList.Tutorial.Intro_1);
    }

    private void Register(Dialogue dialogue)
    {
        if (dialogue != null && !dialogueLookup.ContainsKey(dialogue.dialogueID))
        {
            dialogueLookup.Add(dialogue.dialogueID, dialogue);
        }
        else
        {
            Debug.LogWarning($"Dialogue ID already registered or null: {dialogue?.dialogueID}");
        }
    }

    public Dialogue GetDialogueByID(string dialogueID)
    {
        if (dialogueLookup.TryGetValue(dialogueID, out Dialogue dialogue))
        {
            return dialogue;
        }

        Debug.LogWarning($"Dialogue ID not found: {dialogueID}");
        return null;
    }

}
