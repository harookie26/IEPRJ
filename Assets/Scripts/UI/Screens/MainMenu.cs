using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EventNames.GameStateEvents;

[FoldableInspector]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingsPanel;

    private ScreenFader _screenFader;
    private SceneLoader _sceneLoader;

    private GameStateManager _gameState;

    private bool _settingsOpen = false;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        _screenFader = FindFirstObjectByType<ScreenFader>();
        _screenFader.StartCoroutine(_screenFader.FadeInSequence(1.0f));

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playButton != null)
        {
            playButton.onClick.AddListener(() => _sceneLoader.LoadSceneByName(SceneNames.GameScene));
        }
        else
        {
            Debug.LogError("Play button is not assigned in the inspector.");
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => Application.Quit());
        }
        else
        {
            Debug.LogError("No button is not assigned in the inspector.");
        }

        _sceneLoader = FindFirstObjectByType<SceneLoader>();
        if (_sceneLoader == null)
        {
            Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");
        }

        if(SceneManager.GetActiveScene().name != "MainMenu")
        {
            _gameState = FindFirstObjectByType<GameStateManager>();
            if (_gameState == null)
            {
                Debug.LogError("GameStateManager not found in the scene. Please add one and assign it.");
            }
        }   
    }

    public void ToggleSettings()
    {
        if (_settingsOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    private void OpenSettings()
    {
        // Open settings and hide pause visually but keep game paused
        _settingsOpen = true;
        if (settingsPanel != null) settingsPanel.SetActive(true);

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            _gameState.PauseGame();
        }

        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void CloseSettings()
    {
        _settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Keep game paused while back on pause menu

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            _gameState.PauseGame();
        }

        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        if (_settingsOpen || SceneManager.GetActiveScene().name == "MainMenu")
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
