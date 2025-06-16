using UnityEngine;
using static EventNames;

public class SceneHandler : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    private SceneLoader sceneLoader;

    private bool canSwitchScene = false;

    void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(UIEvents.HOVER_UI_SHOWN, OnHoverUIShown);
        EventBroadcaster.Instance.AddObserver(UIEvents.HOVER_UI_HIDDEN, OnHoverUIHidden);
    }

    void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(UIEvents.HOVER_UI_SHOWN, OnHoverUIShown);
        EventBroadcaster.Instance.RemoveActionAtObserver(UIEvents.HOVER_UI_HIDDEN, OnHoverUIHidden);
    }

    void OnHoverUIShown()
    {
        canSwitchScene = true;
    }

    void OnHoverUIHidden()
    {
        canSwitchScene = false;
    }

    void Start()
    {
        // Optionally, auto-assign SceneLoader if not set in Inspector
        if (sceneLoader == null)
        {
            sceneLoader = FindFirstObjectByType<SceneLoader>();
            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");
            }
        }
    }

    void Update()
    {
        SwitchTo2DScene();
    }

    void SwitchTo2DScene()
    {
        if (canSwitchScene && Input.GetKeyDown(KeyCode.E))
        {
            if (sceneLoader != null)
            {
                // Broadcast event before switching scene
                EventBroadcaster.Instance.PostEvent(EventNames.SceneEvents.ON_SCENE_SWITCH);

                sceneLoader.LoadSceneByName(SceneNames.GameScene);
            }
            else
            {
                Debug.LogError("SceneLoader reference is missing.");
            }
        }
    }
}
