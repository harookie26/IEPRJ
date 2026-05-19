using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

public class ReviewerToolsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject reviewerMenuPanel;

    [Header("Behavior")]
    private KeyCode toggleKeyControl1 = KeyCode.LeftAlt;
    private KeyCode toggleKeyControl2 = KeyCode.RightAlt;
    private KeyCode MenutoggleKey = KeyCode.Q;

    private bool isVisible = true;

    CheckpointSelectManager checkpointSelectManager;
    PerformanceOverlay performanceOverlay;
    BuildVersionToggle buildVersionToggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        checkpointSelectManager = FindObjectOfType<CheckpointSelectManager>();
        performanceOverlay = FindObjectOfType<PerformanceOverlay>();
        buildVersionToggle = FindObjectOfType<BuildVersionToggle>();

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
    }

    public void ToggleReviewerMenu()
    {
        isVisible = !isVisible;
        reviewerMenuPanel.SetActive(isVisible);
        UpdateCursorVisibility();
    }

    public void OnGameRestart()
    {
        Time.timeScale = 1f;
        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME); // optional safety
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnGameSaved()
    {
        // Placeholder for any actions to take when the game is saved, such as updating UI or logging.
    }

    public void OnCheckpointSelect(int index)
    {

        CheckpointSelectManager checkpointSelectManager = FindObjectOfType<CheckpointSelectManager>();
        checkpointSelectManager.LoadCheckpoint(index);
    }

    private void UpdateCursorVisibility()
    {
        if (isVisible || SceneManager.GetActiveScene().name == "MainMenu")
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

    private void HandleKeyToggles()
    {
        if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.F))
            if (performanceOverlay != null) performanceOverlay.ToggleFPS();

        if ((Input.GetKey(toggleKeyControl1) || Input.GetKey(toggleKeyControl2)) && Input.GetKeyDown(KeyCode.B))
            if (buildVersionToggle != null) buildVersionToggle.ToggleBuildVersion();


        if (reviewerMenuPanel != null )
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
