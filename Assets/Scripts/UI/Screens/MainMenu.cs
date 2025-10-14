using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Settings UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject settingsCloseButton;

    private SceneLoader sceneLoader;

    private void Start()
    {
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

        if (settingsButton != null)
        {
            Debug.LogWarning("Settings button functionality is not implemented yet.");
        }
        else
        {
            Debug.LogError("Settings button is not assigned in the inspector.");
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

    public void OnSettingsToggled()
    {
        if (settingsPanel != null && settingsCloseButton != null)
        {
            bool isActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isActive);
            settingsCloseButton.SetActive(!isActive);
        }
        else
        {
            Debug.LogError("Settings panel or close button is not assigned in the inspector.");
        }
    }
}
