using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [SerializeField] private Button exitButton;

    private SceneLoader sceneLoader;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.HubScene));
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
