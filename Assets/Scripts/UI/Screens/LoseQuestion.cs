using UnityEngine;
using UnityEngine.UI;

public class LoseQuestion : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private SceneLoader sceneLoader;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.LevelSelection));
        }
        else
        {
            Debug.LogError("Yes button is not assigned in the inspector.");
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(() => sceneLoader.LoadSceneByName(SceneNames.GameOverScene));
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
