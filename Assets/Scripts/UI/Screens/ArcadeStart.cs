using UnityEngine;
using UnityEngine.UI;

public class ArcadeStart : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private SceneLoader sceneLoader;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogError("Start button is not assigned in the inspector.");
        }

        if (sceneLoader == null)
        {
            sceneLoader = FindFirstObjectByType<SceneLoader>();
            if (sceneLoader == null)
                Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");

        }

    }

    private void OnStartButtonClicked()
    {
        sceneLoader.LoadSceneByName(SceneNames.LevelSelection);

    }
}
