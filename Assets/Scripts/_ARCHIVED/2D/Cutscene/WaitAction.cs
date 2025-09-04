using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class WaitAction : CutsceneAction
{
    public override void Execute(Action onComplete)
    {
        // The InputManager is a MonoBehaviour, so it can run coroutines for us.
        if (InputManager.Instance != null)
        {
            InputManager.Instance.StartCoroutine(InputManager.Instance.WaitForInputCoroutine(onComplete));
        }
        else
        {
            Debug.LogWarning("InputManager instance not found. Cannot perform WaitAction. Completing immediately.");
            onComplete();
        }
    }
}