using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;

    private ScreenFader screenFader;
    private SceneLoader sceneLoader;

    private void Start()
    {
        screenFader = FindFirstObjectByType<ScreenFader>();
        screenFader.StartCoroutine(screenFader.FadeInSequence(1.0f));

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
