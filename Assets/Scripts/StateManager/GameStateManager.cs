using UnityEngine;
using static EventNames.GameStateEvents;
using UnityEngine.SceneManagement;
using System.Collections;

[FoldableInspector]
public class GameStateManager : MonoBehaviour
{
    private bool _isGamePaused;

    private InputManager _inputManager;

    private bool _debugMode = false;

    private EnemyStateMachine _enemy;

    private PaintbrushChanneller paintbrushChanneller;

    private DialogueTriggerManager dialogueTriggerManager;

    private ScreenFader _screenFader => FindFirstObjectByType<ScreenFader>();

    private SceneLoader _sceneLoader => FindFirstObjectByType<SceneLoader>();

    public int currentLevelProgress = 0;

    private bool _isWinSequenceRunning = false;

    private void Awake()
    {
        _inputManager = InputManager.Instance;
        _enemy = FindFirstObjectByType<EnemyStateMachine>();
        paintbrushChanneller = FindFirstObjectByType<PaintbrushChanneller>();
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
        _isGamePaused = false;

        dialogueTriggerManager = FindAnyObjectByType<DialogueTriggerManager>();

        StartCoroutine(StartIntroDialogue());
    }

    private IEnumerator StartIntroDialogue()
    {
        yield return new WaitForSeconds(0.25f);
        if (dialogueTriggerManager != null)
        {
            dialogueTriggerManager.TriggerIntroDialogue();
        }
    }

    void Update()
    {
        if (_inputManager == null)
            _inputManager = InputManager.Instance;

        if (_inputManager != null && _inputManager.WasDebugModePressed())
        {
            _debugMode = !_debugMode;
            if (_debugMode)
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
        if (_isGamePaused) return;

        _isGamePaused = true;

        Time.timeScale = 0f;

        if (_enemy != null && _enemy.isEnemyActivated)
            _enemy.Freeze();
    }

    public void ResumeGame()
    {
        if (!_isGamePaused) return;

        _isGamePaused = false;

        Time.timeScale = 1f;

        if (_enemy != null && _enemy.isEnemyActivated)
            _enemy.Unfreeze();
    }

    public void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("Hub");
        PlayerPrefs.Save();
    }

    private IEnumerator WinGameSequence()
    {
        if (_isWinSequenceRunning)
            yield break;

        _isWinSequenceRunning = true;

        Time.timeScale = 1f;

        Debug.Log("[GameStateManager] Starting WinGameSequence. screenFader=" + (_screenFader != null) + ", sceneLoader=" + (_sceneLoader != null));

        if (_screenFader != null)
        {
            yield return _screenFader.FadeOutSequence(0.5f);
        }
        else
        {
            Debug.LogWarning("[GameStateManager] No ScreenFader found in scene. Skipping fade.");
        }

        float postFadeDelay = 0.25f;
        if (postFadeDelay > 0f)
            yield return new WaitForSecondsRealtime(postFadeDelay);

        if (_sceneLoader != null)
        {
            _sceneLoader.LoadSceneByName("MainMenu");
        }
        else
        {
            Debug.LogWarning("[GameStateManager] No SceneLoader found. Using SceneManager.LoadScene fallback.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void TriggerWinSequence()
    {
        if (_isWinSequenceRunning)
        {
            Debug.Log("[GameStateManager] Win sequence already running. Ignoring TriggerWinSequence call.");
            return;
        }

        StartCoroutine(WinGameSequence());
    }

    private void UpdateLevelProgress()
    {
        if (paintbrushChanneller != null)
        {
            currentLevelProgress = paintbrushChanneller.GetChannelledPaintingCount();
        }
    }

    public int GetCurrentLevelProgress()
    {
        return currentLevelProgress;
    }
}
