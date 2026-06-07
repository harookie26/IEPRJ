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

        ApplyGameplayCursorLock();

        if (playerInput != null)
        {
            playerInput.ActivateInput();

            if (!string.IsNullOrEmpty(gameplayActionMap))
            {
                playerInput.SwitchCurrentActionMap(gameplayActionMap);
                playerInput.currentActionMap?.Enable();
            }
        }

        // Some platforms apply their own cursor state after the focus callback.
        yield return new WaitForEndOfFrame();

        if (Application.isFocused)
            ApplyGameplayCursorLock();

        Debug.Log("[Focus] Restored gameplay input and cursor lock.");
    }

    private void ApplyGameplayCursorLock()
    {
        if (!lockCursorDuringGameplay)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
