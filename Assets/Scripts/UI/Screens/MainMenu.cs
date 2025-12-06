using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EventNames.GameStateEvents;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingsPanel;

    private ScreenFader screenFader;
    private SceneLoader sceneLoader;

    private GameStateManager gameState => FindFirstObjectByType<GameStateManager>();

    private bool settingsOpen = false;
    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        screenFader = FindFirstObjectByType<ScreenFader>();
        screenFader.StartCoroutine(screenFader.FadeInSequence(1.0f));

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playButton != null)
        {
            playButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.GameScene));
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

        sceneLoader = FindFirstObjectByType<SceneLoader>();
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");
        }
    }

    public void ToggleSettings()
    {
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


        gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void CloseSettings()
    {
        settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Keep game paused while back on pause menu
        gameState.PauseGame();
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);

        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        if (settingsOpen || SceneManager.GetActiveScene().name == "MainMenu")
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
