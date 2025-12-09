using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

[FoldableInspector]
public class PauseScript : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private GameStateManager _gameState => FindFirstObjectByType<GameStateManager>();

    private SceneLoader _sceneLoader => FindFirstObjectByType<SceneLoader>();

    private bool _pauseOpen = false;
    private bool _settingsOpen = false;

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
            if (_settingsOpen)
            {
                CloseSettings();
            }
            else if (_pauseOpen)
            {
                ClosePause();
            }
            else
            {
                OpenPause();
            }
        }
    }

    public void ReturnToMainMenu()
    {
        _sceneLoader.LoadSceneByName(SceneNames.MainMenu);
    }

    public void TogglePause()
    {
        if (_pauseOpen)
            ClosePause();
        else
            OpenPause();
    }

    private void OpenPause()
    {
        _pauseOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        _settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        _gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void ClosePause()
    {
        _pauseOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (!_settingsOpen)
        {
            _gameState.ResumeGame();
            EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        }
        else
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        UpdateCursorVisibility();
    }

    public void ToggleSettings()
    {
        if (!_pauseOpen && !_settingsOpen)
        {
            return;
        }

        if (_settingsOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    private void OpenSettings()
    {
        _settingsOpen = true;
        if (settingsPanel != null) settingsPanel.SetActive(true);

        _pauseOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        _gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void CloseSettings()
    {
        _settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        _pauseOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        _gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        if (_settingsOpen || _pauseOpen || SceneManager.GetActiveScene().name == "MainMenu")
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