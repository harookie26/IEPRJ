using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

public class ReviewerToolsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject reviewerMenuPanel;
    [SerializeField] private GameObject gameSavedText;

    [Header("Behavior")]
    private KeyCode toggleKeyControl1 = KeyCode.LeftAlt;
    private KeyCode toggleKeyControl2 = KeyCode.RightAlt;
    private KeyCode MenutoggleKey = KeyCode.Q;

    private bool isVisible = true;
    private bool isGamePaused = false;

    GameStateManager gameStateManager;
    PerformanceOverlay performanceOverlay;
    BuildVersionToggle buildVersionToggle;

    PlayerCamera playerCamera;
    PlayerMovement playerMovement;

    SaveManager saveManager;
    CheckpointManager checkpointManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
        performanceOverlay = FindObjectOfType<PerformanceOverlay>();
        buildVersionToggle = FindObjectOfType<BuildVersionToggle>();
        saveManager = FindObjectOfType<SaveManager>();
        checkpointManager = FindObjectOfType<CheckpointManager>();

        playerCamera = FindObjectOfType<PlayerCamera>();
        playerMovement = FindObjectOfType<PlayerMovement>();

        if (reviewerMenuPanel != null)
        {
            isVisible = false;
            reviewerMenuPanel.SetActive(isVisible);
        }
    }


    // Update is called once per frame
    void Update()
    {
        HandleKeyToggles();

        if (playerCamera == null || playerMovement == null) return;

    }

    public void ToggleReviewerMenu()
    {
        isVisible = !isVisible;
        reviewerMenuPanel.SetActive(isVisible);
        gameStateManager.UpdateCursorVisibility(isVisible);

        if (isVisible == true)
        {
            EventBroadcaster.Instance.PostEvent(ON_GAME_PAUSE);
        }
        else
        {
            EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
        }

    }

    public void OnGameRestart()
    {
        Time.timeScale = 1f;
        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

        isVisible = false;
        if (reviewerMenuPanel != null)
            reviewerMenuPanel.SetActive(false);

        gameStateManager?.UpdateCursorVisibility(false);

        if (checkpointManager == null)
            checkpointManager = FindObjectOfType<CheckpointManager>();

        if (checkpointManager != null)
        {
            checkpointManager.StartReturnToCheckpoint();
        }
        else
        {
            Debug.LogError("[ReviewerTools] Cannot respawn: CheckpointManager was not found.");
        }
    }

    public void OnGameSaved()
    {
        saveManager.ToggleSaveGame("save1");
        if (gameSavedText != null)
        {
            gameSavedText.SetActive(true);
            Invoke("HideGameSavedText", 2f); // Hide the text after 2 seconds
        }
    }

    private void HideGameSavedText()
    {
        if (gameSavedText != null)
        {
            gameSavedText.SetActive(false);
        }
    }


    private void HandleKeyToggles()
    {
        if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.F))
            if (performanceOverlay != null) performanceOverlay.ToggleFPS();

        if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.B))
            if (buildVersionToggle != null) buildVersionToggle.ToggleBuildVersion();


        if (reviewerMenuPanel != null)
        {
            if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(MenutoggleKey))
                ToggleReviewerMenu();

            if (SceneManager.GetActiveScene().name == "Main")
            {
                if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.R))
                    OnGameRestart();

                if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.S))
                    OnGameSaved();
            }

        }
    }

}
