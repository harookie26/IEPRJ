using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

[FoldableInspector]
public class PauseScript : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject saveGamePanel;
    [SerializeField] private GameObject confirmSavePanel;
    [SerializeField] private GameObject gameSavedText;

    public string currentSaveSlot = "save1";

    private GameStateManager _gameState => FindFirstObjectByType<GameStateManager>();

    private SaveManager _saveManager => FindFirstObjectByType<SaveManager>();

    private SceneLoader _sceneLoader => FindFirstObjectByType<SceneLoader>();

    private bool _pauseOpen = false;
    private bool _settingsOpen = false;
    private bool _saveGameOpen = false;
    private bool _confirmSaveOpen = false;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if(saveGamePanel != null) saveGamePanel.SetActive(false);
        if(confirmSavePanel != null) confirmSavePanel.SetActive(false);
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
            if (_saveGameOpen)
            {
                CloseSaveGame();
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

    public void SetSaveSlot(string slot)
    {
        currentSaveSlot = slot;
        confirmSavePanel.SetActive(true);
    }

    public void SaveGame()
    {
        _saveManager.SaveGameAsync(currentSaveSlot);
        confirmSavePanel.SetActive(false);
        if (gameSavedText != null)
        {
            gameSavedText.SetActive(true);
            Invoke("HideGameSavedText", 2f); //
        }
    }

    private void HideGameSavedText()
    {
        if (gameSavedText != null)
        {
            gameSavedText.SetActive(false);
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

        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE); // GameStateManager handles the rest
        _gameState.UpdateCursorVisibility(_pauseOpen);
    }

    private void ClosePause()
    {
        _pauseOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        _gameState.UpdateCursorVisibility(_pauseOpen);
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

        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);
        _gameState.UpdateCursorVisibility(_settingsOpen);
    }

    private void CloseSettings()
    {
        _settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        _pauseOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ToggleSaveGame()
    {
        if (!_pauseOpen && !_saveGameOpen)
        {
            return;
        }
        if (_saveGameOpen)
            CloseSaveGame();
        else
            OpenSaveGame();
    }

    private void OpenSaveGame()
    {
        _saveGameOpen = true;
        if (saveGamePanel != null) saveGamePanel.SetActive(true);
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);
        _gameState.UpdateCursorVisibility(_saveGameOpen);
    }

    private void CloseSaveGame()
    {
        _saveGameOpen = false;
        confirmSavePanel.SetActive(false);
        if (saveGamePanel != null) saveGamePanel.SetActive(false);
    }

}