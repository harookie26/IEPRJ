using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class WaitAction : CutsceneAction
{
    public override void Execute(Action onComplete)
    {
        // The CutsceneManager is a MonoBehaviour, so it can run coroutines for us.
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.StartCoroutine(WaitForInputCoroutine(onComplete));
        }
        else
        {
            Debug.LogWarning("CutsceneManager instance not found. Cannot perform WaitAction. Completing immediately.");
            onComplete();
        }
    }

    private IEnumerator WaitForInputCoroutine(Action onComplete)
    {
        // Wait until the end of the current frame.
        // This prevents capturing the same input that triggered this action.
        yield return new WaitForEndOfFrame();

        // Now, loop every frame until a new input is detected.
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButtonDown(0))
        {
            yield return null; // Wait for the next frame before checking again.
        }
        
        // Input was detected, so call the onComplete callback to proceed.
        onComplete();
    }
}