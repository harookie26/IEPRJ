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
        // Always start from DialogueMasterList
        // Tutorial dialogues
        Register(DialogueMasterList.Tutorial.Intro_1);

        // ObjectiveMechanics_Cinematic dialogues
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.MOVE_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.WALK_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.INTERACT_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.NOTHING_MOVES_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.HIDE_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.TAB_TAKEOVER_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.GUIDE_OPERATE_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.TAB_RETURN_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.OBJECTIVE_DONE_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.ALL_DONE_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.GHOST_NEAR_CINEMATIC);
        Register(DialogueMasterList.ObjectiveMechanics_Cinematic.ACCESSIBLE_CINEMATIC);
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
