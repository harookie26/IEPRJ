using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    private SceneLoader sceneLoader;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playButton != null)
        {
            playButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.HubScene));
        }
        else
        {
            Debug.LogError("Play button is not assigned in the inspector.");
        }
        if (settingsButton != null)
        {
            //settingsButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.SettingsScene));
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
}
