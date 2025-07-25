using System;
using System.Collections;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        // Standard singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public IEnumerator WaitForInputCoroutine(Action onComplete)
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
