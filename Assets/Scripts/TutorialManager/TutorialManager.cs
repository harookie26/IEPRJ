using System;
using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private FadeElement[] _tutorialPanels;

    // Triggers the full sequence (Fade In -> Stay Visible -> Fade Out) for a panel

    private void Start()
    {
        TriggerMovementTutorial();
        TriggerFlashlightTutorial();
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
            if (panel != null)
            {
                panel.ForceHide();
            }
        }
    }

    public void TriggerMovementTutorial()
    {
        StartCoroutine(TriggerTutorialWithDelay(0, 4.0f)); ;
    }

    public void TriggerFlashlightTutorial()
    {
        StartCoroutine(TriggerTutorialWithDelay(1, 10.0f)); ;
    }
    public void TriggerChannelingTutorial()
    {
        StartCoroutine(TriggerTutorialWithDelay(2, 4.0f)); ;
    }
}