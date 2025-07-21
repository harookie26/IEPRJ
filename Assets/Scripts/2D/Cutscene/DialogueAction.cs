using System;
using UnityEngine;

[Serializable]
public class DialogueAction : CutsceneAction
{
    public string characterName;
    [TextArea]
    public string dialogueLine;

    public override void Execute(Action onComplete)
    {
        // Find the DialogueManager in the scene
        DialogueManager dialogueManager = GameObject.FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            // The DialogueManager will be responsible for calling onComplete
            dialogueManager.ShowDialogue(characterName, dialogueLine, onComplete);
        }
        else
        {
            Debug.LogWarning("DialogueManager not found in the scene.");
            // If no manager is found, log the dialogue and complete the action immediately
            Debug.Log($"{characterName}: {dialogueLine}");
            onComplete();
        }
    }
}