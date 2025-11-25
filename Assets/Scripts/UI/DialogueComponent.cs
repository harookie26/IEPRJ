using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static EventNames;

[RequireComponent(typeof(BoxCollider))]


public class DialogueComponent : MonoBehaviour
{
    [Tooltip("List of dialogue IDs this object will play in order.")]
    [SerializeField] private string[] dialogueIDs;

    private int currentIndex = 0;

    private void Awake()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("I AM HERE NOW LMAO");

        if (dialogueIDs == null || dialogueIDs.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name} has no dialogue IDs assigned.");
            return;
        }

        if (currentIndex >= dialogueIDs.Length)
        {
            Debug.Log($"{gameObject.name} has no more dialogues to play.");
            return;
        }

        string currentID = dialogueIDs[currentIndex];
        Dialogue dialogue = DialogueRegistryManager.Instance.GetDialogueByID(currentID);

        if (dialogue == null)
        {
            Debug.LogWarning($"Dialogue ID '{currentID}' not found in registry.");
            return;
        }

        if (DialogueManager.Instance.HasCompletedDialogue(currentID))
        {
            Debug.Log($"Dialogue '{currentID}' already completed.");
            return;
        }

        DialogueManager.Instance.ShowDialogue(dialogue, () =>
        {
            currentIndex++;
        });
    }

}
