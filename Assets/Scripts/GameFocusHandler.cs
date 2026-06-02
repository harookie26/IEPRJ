using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameFocusHandler : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    [Header("Action Maps")]
    [SerializeField] private string gameplayActionMap = "PlayerControls";

    [Header("Cursor")]
    [SerializeField] private bool lockCursorDuringGameplay = true;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            StartCoroutine(RestoreGameplayInputNextFrame());
        }
        else
        {
            ReleaseGameplayFocus();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            StartCoroutine(RestoreGameplayInputNextFrame());
        }
        else
        {
            ReleaseGameplayFocus();
        }
    }

    private IEnumerator RestoreGameplayInputNextFrame()
    {
        // Wait one frame so Unity/InputSystem finishes processing focus regain.
        yield return null;

        if (!Application.isFocused)
            yield break;

        if (lockCursorDuringGameplay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerInput != null)
        {
            playerInput.ActivateInput();

            if (!string.IsNullOrEmpty(gameplayActionMap))
            {
                playerInput.SwitchCurrentActionMap(gameplayActionMap);
                playerInput.currentActionMap?.Enable();
            }
        }

        Debug.Log("[Focus] Restored gameplay input and cursor lock.");
    }

    private void ReleaseGameplayFocus()
    {
        if (playerInput != null)
        {
            // Prevent held keys/buttons from staying logically active after alt-tab.
            playerInput.DeactivateInput();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[Focus] Released gameplay input.");
    }
}