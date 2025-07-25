using UnityEngine;
using System.Collections.Generic;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using static EventNames.CutsceneEvents;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private Animator cinematicBarsAnimator;
    [SerializeField] private Animator cameraAnimator;

    public static CutsceneManager Instance { get; private set; }

    private bool isCutsceneActive = false;
    private Queue<CutsceneAction> actionQueue;
    private string activeCutsceneId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        actionQueue = new Queue<CutsceneAction>();
    }

    public void PlayCutscene(string cutsceneName)
    {
        if (isCutsceneActive)
        {
            Debug.LogWarning($"Cannot start cutscene '{cutsceneName}'. Another cutscene ('{activeCutsceneId}') is already active.");
            return;
        }

        CutsceneSequence sequence = LoadCutsceneFromFile(cutsceneName);
        if (sequence != null)
        {
            activeCutsceneId = cutsceneName;
            OnCutsceneStart(sequence);
        }
        else
        {
            Debug.LogError($"Failed to load cutscene with name '{cutsceneName}'.");
        }
    }

    private CutsceneSequence LoadCutsceneFromFile(string cutsceneName)
    {
        var textAsset = Resources.Load<TextAsset>($"Cutscenes/{cutsceneName}");
        if (textAsset == null)
        {
            Debug.LogError($"Cutscene file '{cutsceneName}.yml' not found in any Resources/Cutscenes folder.");
            return null;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTagMapping("!dialogue", typeof(DialogueAction))
            .WithTagMapping("!cameraFocus", typeof(CameraFocusAction))
            .WithTagMapping("!animation", typeof(AnimationAction))
            .WithTagMapping("!wait", typeof(WaitAction))
            .Build();

        try
        {
            return deserializer.Deserialize<CutsceneSequence>(textAsset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deserializing cutscene '{cutsceneName}': {e.Message}");
            return null;
        }
    }

    private void OnCutsceneStart(CutsceneSequence sequence)
    {
        EventBroadcaster.Instance.PostEvent(CUTSCENE_START);

        Debug.Log($"Cutscene '{activeCutsceneId}' started.");
        isCutsceneActive = true;
        // GameState.IsCutsceneActive = true; // Example state change

        foreach (var action in sequence.actions)
        {
            if (action == null)
            {
                Debug.LogError("A null action was found in the cutscene sequence. Check your YAML file for incorrect or unmapped tags.");
                continue;
            }
            Debug.Log($"Enqueuing action: {action.GetType().Name}");
            actionQueue.Enqueue(action);
        }
        
        ProcessNextAction();
    }

    private void ProcessNextAction()
    {
        if (actionQueue.Count > 0)
        {
            CutsceneAction currentAction = actionQueue.Dequeue();
            Debug.Log($"Executing action: {currentAction.GetType().Name}");
            currentAction.Execute(ProcessNextAction);
        }
        else
        {
            OnCutsceneEnd();
        }
    }

    private void OnCutsceneEnd()
    {
        if (!isCutsceneActive) return;

        EventBroadcaster.Instance.PostEvent(CUTSCENE_END);

        Debug.Log($"Cutscene '{activeCutsceneId}' ended.");
        isCutsceneActive = false;
        activeCutsceneId = null;

        if (cameraAnimator != null)
        {
            cameraAnimator.SetTrigger("cutsceneEnd");
        }

        if (cinematicBarsAnimator != null)
        {
            cinematicBarsAnimator.SetTrigger("hide");
        }
    }
}
