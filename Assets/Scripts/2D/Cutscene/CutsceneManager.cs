using UnityEngine;
using static EventNames;
using System.Collections.Generic;
using System;
using System.Collections;
using Unity.Cinemachine;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private Animator cinematicBarsAnimator;
    [SerializeField] private Animator cameraAnimator;

    public static CutsceneManager Instance { get; private set; }

    [Serializable]
    public struct NamedCutsceneSequence
    {
        public string cutsceneId;
        public CutsceneSequence sequence;
    }

    public List<NamedCutsceneSequence> cutsceneSequences;
    private Dictionary<string, CutsceneSequence> cutsceneDictionary;

    private bool isCutsceneActive = false;
    private Queue<CutsceneAction> actionQueue;
    private CutsceneAction currentAction;
    private string activeCutsceneId;

    public CinemachineFollow cinemachineFollow;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cinemachineFollow = FindAnyObjectByType<CinemachineFollow>();

        // Populate the dictionary for fast lookups
        cutsceneDictionary = new Dictionary<string, CutsceneSequence>();
        foreach (var namedSequence in cutsceneSequences)
        {
            if (!string.IsNullOrEmpty(namedSequence.cutsceneId) && namedSequence.sequence != null)
            {
                cutsceneDictionary[namedSequence.cutsceneId] = namedSequence.sequence;
            }
        }
    }

    public void PlayCutscene(string cutsceneId)
    {
        if (isCutsceneActive)
        {
            Debug.LogWarning($"Cannot start cutscene '{cutsceneId}'. Another cutscene ('{activeCutsceneId}') is already active.");
            return;
        }

        if (cutsceneDictionary.TryGetValue(cutsceneId, out CutsceneSequence sequenceToPlay))
        {
            activeCutsceneId = cutsceneId;
            OnCutsceneStart(sequenceToPlay);
        }
        else
        {
            Debug.LogError($"Cutscene with ID '{cutsceneId}' not found.");
        }
    }

    private void OnCutsceneStart(CutsceneSequence sequence)
    {
        Debug.Log($"Cutscene '{activeCutsceneId}' started.");
        isCutsceneActive = true;
        GameState.IsCutsceneActive = true;

        actionQueue = new Queue<CutsceneAction>(sequence.actions);
        EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_START);
        ProcessNextAction();
    }

    private void ProcessNextAction()
    {
        // Check if the last action was a dialogue action, which requires input.
        bool wasDialogue = currentAction is DialogueAction;

        if (actionQueue.Count > 0)
        {
            currentAction = actionQueue.Dequeue();
            if (wasDialogue)
            {
                // If the last action was dialogue, wait a frame before the next action.
                StartCoroutine(ProcessNextActionAfterDelay());
            }
            else
            {
                currentAction.Execute(ProcessNextAction);
            }
        }
        else
        {
            OnCutsceneEnd();
        }
    }

    private IEnumerator ProcessNextActionAfterDelay()
    {
        yield return null; // Wait for the next frame.

        currentAction.Execute(ProcessNextAction);
    }

    private void OnCutsceneEnd()
    {
        cinemachineFollow.enabled = true; // Re-enable camera follow after cutscene ends

        if (!isCutsceneActive) return;

        Debug.Log($"Cutscene '{activeCutsceneId}' ended.");
        isCutsceneActive = false;
        GameState.IsCutsceneActive = false;
        actionQueue?.Clear();
        currentAction = null;
        activeCutsceneId = null;

        cameraAnimator.SetTrigger("cutsceneEnd");

        if (cinematicBarsAnimator != null)
        {
            cinematicBarsAnimator.SetTrigger("hide");
        }

        EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_END);
    }
}
