using Level.UI;
using System.Collections.Generic;
using UnityEngine;

public class DialogueIntroSceneManager : MonoBehaviour
{
    [Header("Timed Voice Sequence")]
    [SerializeField, Tooltip("Optional timed voice sequence used for the intro. If unassigned, Intro Dialogues are used instead.")]
    private VoicedDialogueSequence introVoiceSequence;

    [Header("Fallback Dialogue")]
    [SerializeField] private List<DialogueEntry> introDialogues;

    private void Start()
    {
        if (introVoiceSequence != null)
        {
            DialogueManager.Instance.DisplaySequence(introVoiceSequence);
            return;
        }

        DialogueManager.Instance.DisplaySequence(introDialogues);
    }
}
