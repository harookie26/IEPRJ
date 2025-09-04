using System;
using UnityEngine;

[Serializable]
public class DialogueAction : CutsceneAction
{
    // Renamed from characterName and dialogueLine to match convention
    public string CharacterName { get; set; } 
    [TextArea]
    public string DialogueLine { get; set; }

    public override void Execute(Action onComplete)
    {
        // Find the DialogueManager in the scene
        DialogueManager dialogueManager = GameObject.FindFirstObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            // The DialogueManager will be responsible for calling onComplete
            dialogueManager.ShowDialogue(CharacterName, DialogueLine, onComplete);
        }
        else
        {
            Debug.LogWarning("DialogueManager not found in the scene.");
            // If no manager is found, log the dialogue and complete the action immediately
            Debug.Log($"{CharacterName}: {DialogueLine}");
            onComplete();
        }
    }
}