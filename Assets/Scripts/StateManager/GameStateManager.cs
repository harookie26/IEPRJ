using UnityEngine;
using static EventNames.GameStateEvents;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class GameStateManager : MonoBehaviour
{
    private bool isGamePaused;

    private InputManager inputManager;

    private bool debugMode = false;

    private void Awake()
    {
        inputManager = InputManager.Instance;
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
    }

    private void PauseGame()
    {
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);
        Time.timeScale = 0.00000001f;
    }

    private void ResumeGame()
    {
        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        Time.timeScale = 1f;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("Hub");
        PlayerPrefs.Save();
    }
}
