using UnityEngine;
using static EventNames;

public class SceneHandler : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    private SceneLoader sceneLoader;

    private Transform playerTransform;

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
        if (sceneLoader == null)
        {
            sceneLoader = FindFirstObjectByType<SceneLoader>();
            if (sceneLoader == null)
                Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");
            
        }

        playerTransform = GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        SwitchTo2DScene();
    }

    void SwitchTo2DScene()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not found. Make sure a GameObject with the 'Player' tag exists in the scene.");
            return;
        }

        SceneStateData sceneStateData = new SceneStateData
        {
            playerPosition = playerTransform.position,
            playerRotation = transform.rotation
        };

        PlayerPrefs.SetString("Hub", JsonUtility.ToJson(sceneStateData));
        PlayerPrefs.Save();

        if (canSwitchScene && Input.GetKeyDown(KeyCode.E))
        {
            if (sceneLoader != null)
            {
                EventBroadcaster.Instance.PostEvent(SceneEvents.ON_SCENE_SWITCH);

                sceneLoader.LoadSceneByName(SceneNames.ArcadeStart);
            }
            else
            {
                Debug.LogError("SceneLoader reference is missing.");
            }
        }
    }
}
