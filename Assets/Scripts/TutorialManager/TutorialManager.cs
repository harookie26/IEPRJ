using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private FadeElement[] _tutorialPanels;

    // Initialize immediately to prevent any NullReferenceExceptions
    private List<int> triggeredTutorialIDs = new List<int>();

    // The master flag that completely kills the race condition
    private bool hasSaveLoaded = false;

    private IEnumerator Start()
    {
        // Wait until the absolute end of the frame. 
        // This guarantees PlayerSaveHandler has finished all of its Awakes/Starts!
        yield return new WaitForEndOfFrame();

        // If LoadSaveData was NEVER called by the save handler, this is a fresh game!
        if (!hasSaveLoaded)
        {
            Debug.Log("[Tutorial] No save data injected. Starting fresh tutorials.");
            TriggerMovementTutorial();
            TriggerFlashlightTutorial();
        }
    }

    public void TriggerTutorial(int index)
    {
        if (index >= 0 && index < _tutorialPanels.Length)
        {
            _tutorialPanels[index].PlayTutorialSequence();
        }
        else
        {
            Debug.LogWarning($"Tutorial index {index} is out of bounds!");
        }
    }

    private IEnumerator TriggerTutorialWithDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerTutorial(index);
    }

    public void HideAllTutorials()
    {
        foreach (var panel in _tutorialPanels)
        {
            if (panel != null && panel.isActiveAndEnabled)
            {

            }
        }
    }

    public void TriggerMovementTutorial()
    {
        if (triggeredTutorialIDs.Contains(0)) return;

        triggeredTutorialIDs.Add(0);
        StartCoroutine(TriggerTutorialWithDelay(0, 4.0f));
    }

    public void TriggerFlashlightTutorial()
    {
        if (triggeredTutorialIDs.Contains(1)) return;

        triggeredTutorialIDs.Add(1);
        StartCoroutine(TriggerTutorialWithDelay(1, 10.0f));
    }

    public void TriggerChannelingTutorial()
    {
        if (triggeredTutorialIDs.Contains(2)) return;

        triggeredTutorialIDs.Add(2);
        StartCoroutine(TriggerTutorialWithDelay(2, 4.0f));
    }

    public TutorialSaveData GetSaveData()
    {
        return new TutorialSaveData
        {
            triggeredTutorialIDs = new List<int>(this.triggeredTutorialIDs)
        };
    }

    public void LoadSaveData(TutorialSaveData data)
    {
        // 1. Flip the flag! This permanently locks out the fresh start logic in Start()
        hasSaveLoaded = true;

        // 2. Safety cleanup (Crucial if the player loads a save mid-gameplay)
        StopAllCoroutines();
        HideAllTutorials();

        // 3. Apply the saved data state safely
        if (data == null || data.triggeredTutorialIDs == null)
        {
            triggeredTutorialIDs = new List<int>();
        }
        else
        {
            triggeredTutorialIDs = new List<int>(data.triggeredTutorialIDs);
        }

        Debug.Log($"[Save System] Tutorial Data Loaded. Completed IDs: {triggeredTutorialIDs.Count}");

        // 4. Attempt to resume tutorials. 
        // If the save file has [0, 1], the Contains() check in these methods will instantly block them!
        TriggerMovementTutorial();
        TriggerFlashlightTutorial();
    }
}