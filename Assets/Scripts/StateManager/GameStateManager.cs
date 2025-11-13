using UnityEngine;
using static EventNames.GameStateEvents;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class GameStateManager : MonoBehaviour
{
    private bool isGamePaused;

    private InputManager inputManager;

    private bool debugMode = false;

    private EnemyStateMachine enemy;

    private PlayerChanneller playerChanneller;

    public int currentLevelProgress = 0;

    // currentLevelProgress = 0 - no paintings restored
    // currentLevelProgress = 1 - 1 painting restored
    // currentLevelProgress = 2 - 2 paintings restored
    // currentLevelProgress = 3 - 3 paintings restored
    // currentLevelProgress = 4 - 4 paintings restored

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

        if (playerChanneller != null)
        {
            int channeledPaintings = playerChanneller.GetChannelledPaintingCount();
            
            if (channeledPaintings >= 5)
            {
                WinGame();
            }
        }
    }

    private void PauseGame()
    {
        EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);
        Time.timeScale = 0.00000001f;

        if (enemy != null)
            enemy.Freeze();
    }

    private void ResumeGame()
    {
        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        Time.timeScale = 1f;

        if (enemy != null)
            enemy.Unfreeze();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("Hub");
        PlayerPrefs.Save();
    }

    private void WinGame()
    {
        Debug.Log("You Win!");
        Time.timeScale = 0.00000001f;
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
