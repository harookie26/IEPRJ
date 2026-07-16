using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks; // Added for Tasks
using Unity.PlatformToolkit;
using static EventNames.GameStateEvents;

[FoldableInspector]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject noSavedText;

    private ScreenFader _screenFader;
    private SceneLoader _sceneLoader;

    private GameStateManager _gameState;

    private bool _settingsOpen = false;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (noSavedText != null) noSavedText.SetActive(false);

        _screenFader = FindFirstObjectByType<ScreenFader>();
        _screenFader.StartCoroutine(_screenFader.FadeInSequence(1.0f));

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playButton != null)
        {
            playButton.onClick.AddListener(() => playPanel.SetActive(true));
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
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => playPanel.SetActive(false));
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

    // Called when the player clicks "New Game"
    public void StartNewGame()
    {
        SaveCourier.SaveSlotToLoad = ""; // Clear any previous load commands
        //SceneManager.LoadScene("Main"); // Replace with your scene's name
        SceneManager.LoadScene("Intro Cinematic");
    }

    // Called when the player clicks "Load Game"
    public async void LoadGame(string slotName)
    {
        if (noSavedText != null) noSavedText.SetActive(false);

        bool saveExists = await CheckIfSaveExistsAsync(slotName);

        if (saveExists)
        {
            SaveCourier.SaveSlotToLoad = slotName;
            SceneManager.LoadScene("Main");
        }
        else
        {
            Debug.LogError($"Save slot '{slotName}' does not exist. Cannot load game.");
            if (noSavedText != null) noSavedText.SetActive(true);
            Invoke("HideNoSavedText", 2f);
            return;
        }
    }
    private async Task<bool> CheckIfSaveExistsAsync(string slotName)
    {
        try
        {
            await PlatformToolkit.Initialize();

            if (PlatformToolkit.Capabilities.LocalSaving)
            {
                var savingSystem = PlatformToolkit.LocalSaving;
                await using (var readable = await savingSystem.OpenSaveReadable(slotName))
                {
                    return readable != null;
                }
            }
            return false;
        }
        catch
        {
            return false; 
        }
    }

    private void HideNoSavedText()
    {
        if (noSavedText != null)
        {
            noSavedText.SetActive(false);
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

        //EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

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

        //EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

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
