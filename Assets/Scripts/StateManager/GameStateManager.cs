using UnityEngine;
using static EventNames.GameStateEvents;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    private bool isGamePaused;

    private InputManager inputManager;

    private bool debugMode = false;

    private EnemyStateMachine enemy;

    private PlayerChanneller playerChanneller;

    private ScreenFader screenFader => FindFirstObjectByType<ScreenFader>();

    private SceneLoader sceneLoader => FindFirstObjectByType<SceneLoader>();

    public int currentLevelProgress = 0;

    // Guard to ensure the win sequence only runs once
    private bool isWinSequenceRunning = false;

    private void Awake()
    {
        inputManager = InputManager.Instance;
        enemy = FindFirstObjectByType<EnemyStateMachine>();
        playerChanneller = FindFirstObjectByType<PlayerChanneller>();
    }

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(ON_GAME_PAUSE, PauseGame);
        EventBroadcaster.Instance.AddObserver(ON_GAME_RESUME, ResumeGame);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_PAUSE, PauseGame);
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_RESUME, ResumeGame);
    }

    void Start()
    {
        isGamePaused = false;
    }

    void Update()
    {
        if (inputManager == null)
            inputManager = InputManager.Instance;

        if (inputManager != null && inputManager.WasDebugModePressed())
        {
            debugMode = !debugMode;
            if (debugMode)
            {
                EventBroadcaster.Instance.PostEvent(ON_DEBUG_MODE_ON);
                Debug.Log("[GameStateManager] Debug mode ON");
            }
            else
            {
                EventBroadcaster.Instance.PostEvent(ON_DEBUG_MODE_OFF);
                Debug.Log("[GameStateManager] Debug mode OFF");
            }
        }

        UpdateLevelProgress();
    }

    public void PauseGame()
    {
        // Avoid re-entry: this method is the handler for ON_GAME_PAUSE.
        // The event should be posted by the caller (UI / other systems).
        if (isGamePaused) return;

        isGamePaused = true;

        // Use a firm pause. Previously a tiny non-zero value was used; using 0f is clearer.
        Time.timeScale = 0f;

        if (enemy != null)
            enemy.Freeze();
    }

    public void ResumeGame()
    {
        if (!isGamePaused) return;

        isGamePaused = false;

        Time.timeScale = 1f;

        if (enemy != null)
            enemy.Unfreeze();
    }

    public void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("Hub");
        PlayerPrefs.Save();
    }

    private IEnumerator WinGameSequence()
    {
        // Avoid re-entry
        if (isWinSequenceRunning)
            yield break;

        isWinSequenceRunning = true;

        // Ensure timeScale is normal so fade (if implemented with scaled time) can run
        Time.timeScale = 1f;

        Debug.Log("[GameStateManager] Starting WinGameSequence. screenFader=" + (screenFader != null) + ", sceneLoader=" + (sceneLoader != null));

        // If there's a screen fader, run its fade out sequence and wait for it to finish
        if (screenFader != null)
        {
            // Prefer yielding the IEnumerator directly so it's awaited here
            yield return screenFader.FadeOutSequence(0.5f);
        }
        else
        {
            Debug.LogWarning("[GameStateManager] No ScreenFader found in scene. Skipping fade.");
        }

        // Optional extra delay after fade completes (use realtime so it's independent of Time.timeScale)
        float postFadeDelay = 0.25f;
        if (postFadeDelay > 0f)
            yield return new WaitForSecondsRealtime(postFadeDelay);

        // Finally load the main menu
        if (sceneLoader != null)
        {
            sceneLoader.LoadSceneByName("MainMenu");
        }
        else
        {
            Debug.LogWarning("[GameStateManager] No SceneLoader found. Using SceneManager.LoadScene fallback.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Public wrapper so other objects can trigger the win sequence
    public void TriggerWinSequence()
    {
        if (isWinSequenceRunning)
        {
            Debug.Log("[GameStateManager] Win sequence already running. Ignoring TriggerWinSequence call.");
            return;
        }

        StartCoroutine(WinGameSequence());
    }

    private void UpdateLevelProgress()
    {
        if (playerChanneller != null)
        {
            currentLevelProgress = playerChanneller.GetChannelledPaintingCount();
        }
    }

    public int GetCurrentLevelProgress()
    {
        return currentLevelProgress;
    }
}
