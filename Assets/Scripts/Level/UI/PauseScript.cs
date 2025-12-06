using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

public class PauseScript : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private GameStateManager gameState => FindFirstObjectByType<GameStateManager>();

    private SceneLoader sceneLoader => FindFirstObjectByType<SceneLoader>();

    private bool pauseOpen = false;
    private bool settingsOpen = false;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            (SceneManager.GetActiveScene().name == "Main"))
        {
            if (settingsOpen)
            {
                // If in settings, ESC returns to pause menu
                CloseSettings();
            }
            else if (pauseOpen)
            {
                // If in pause (and not settings), ESC resumes game
                ClosePause();
            }
            else
            {
                // If neither open, ESC opens pause menu
                OpenPause();
            }
        }
    }

    public void ReturnToMainMenu()
    {
        sceneLoader.LoadSceneByName(SceneNames.MainMenu);
    }

    // Public for inspector buttons to toggle pause
    public void TogglePause()
    {
        if (pauseOpen)
            ClosePause();
        else
            OpenPause();
    }

    private void OpenPause()
    {
        pauseOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        // Ensure settings panel is not visible when pause is shown
        settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void ClosePause()
    {
        pauseOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        // Only resume game if no UI panels remain open
        if (!settingsOpen)
        {
            gameState.ResumeGame();
            EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        }
        else
        {
            // If settingsOpen true, keep game paused and show settings (defensive)
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        UpdateCursorVisibility();
    }

    // Called by a button on the pause panel. Only allowed when pause is open.
    public void ToggleSettings()
    {
        if (!pauseOpen && !settingsOpen)
        {
            // settings may only be opened from pause menu; guard in case
            return;
        }

        if (settingsOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    private void OpenSettings()
    {
        // Open settings and hide pause visually but keep game paused
        settingsOpen = true;
        if (settingsPanel != null) settingsPanel.SetActive(true);

        pauseOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void CloseSettings()
    {
        settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Return to pause menu
        pauseOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        // Keep game paused while back on pause menu
        gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        if (settingsOpen || pauseOpen || SceneManager.GetActiveScene().name == "MainMenu")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}