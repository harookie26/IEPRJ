using UnityEngine;
using static EventNames;

public class GameStateManager : MonoBehaviour
{
    private bool isGamePaused;
    private bool isLevelFailed;
    private bool isLevelComplete;

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(GameStateEvents.ON_LEVEL_COMPLETE, OnLevelComplete);
        EventBroadcaster.Instance.AddObserver(GameStateEvents.ON_LEVEL_FAILED, OnLevelFailed);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(GameStateEvents.ON_LEVEL_COMPLETE, OnLevelComplete);
        EventBroadcaster.Instance.RemoveActionAtObserver(GameStateEvents.ON_LEVEL_FAILED, OnLevelFailed);
    }

    private void OnLevelComplete()
    {
        isLevelComplete = true;
        Debug.Log("QUEST IS COMPLETE");

    }

    private void OnLevelFailed()
    {
        isLevelFailed = true;
        Debug.Log("Boo!");
    }

    void Start()
    {
        isGamePaused = false;
        isLevelFailed = false;
        isLevelComplete = false;

        //LinkRegistry.Clear();
    }

    void Update()
    {
        if (isLevelFailed || isLevelComplete)
        {
            Time.timeScale = 0f;
        }
    }

    private void PauseGame()
    {
        EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_GAME_PAUSE);
        Time.timeScale = 0.00000001f;
    }

    private void ResumeGame()
    {
        EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_GAME_RESUME);
        Time.timeScale = 1f;
    }
}
