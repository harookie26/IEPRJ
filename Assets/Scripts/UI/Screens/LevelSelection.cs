using UnityEngine;
using UnityEngine.UI;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

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
            Debug.LogError("Start button is not assigned in the inspector.");
        }

        if (leftButton != null)
        {
            leftButton.onClick.AddListener(OnLeftButtonClicked);
        }
        else
        {
            Debug.LogError("Left button is not assigned in the inspector.");
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(OnRightButtonClicked);
        }
        else
        {
            Debug.LogError("Right button is not assigned in the inspector.");
        }

        if (sceneLoader == null)
        {
            sceneLoader = FindFirstObjectByType<SceneLoader>();
            if (sceneLoader == null)
                Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");

        }
    }

    private void OnLeftButtonClicked()
    {
        // Logic for left button click
        Debug.Log("Left button clicked");
        // You can add functionality here, like changing the level selection
    }

    private void OnRightButtonClicked()
    {
        // Logic for right button click
        Debug.Log("Right button clicked");
        // You can add functionality here, like changing the level selection
    }
}
