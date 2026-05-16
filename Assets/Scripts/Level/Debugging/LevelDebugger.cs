using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using static EventNames.GameStateEvents;

public class LevelDebugger : MonoBehaviour
{
    [SerializeField] private GameObject debugPanel;

    [SerializeField] private Toggle playerToggle;
    [SerializeField] private Toggle companionToggle;
    [SerializeField] private Toggle directionalLightToggle;

    [SerializeField] private Button spawnEnemyButton;
    [SerializeField] private Button removeEnemyButton;

    [SerializeField] private Button mainLevelButton;
    [SerializeField] private Button vFXLevelButton;

    [SerializeField] private Button exitButton;

    private GameObject player;
    private GameObject enemy;
    private GameObject companion;

    private SceneLoader sceneLoader;

    private CheckpointManager checkpoint => FindFirstObjectByType<CheckpointManager>();

    private bool suppressToggleEvents = false;

    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        if (debugPanel == null)
            Debug.LogError("Debug Panel is not assigned in the inspector.");
        else
            debugPanel.SetActive(false);

        player = GameObject.FindWithTag("Player");
        if (player == null)
            Debug.LogError("No GameObject with tag 'Player' found in scene.");

        enemy = GameObject.FindWithTag("Enemy");
        if (enemy == null)
            Debug.LogError("No GameObject with tag 'Enemy' found in scene.");

        companion = GameObject.FindWithTag("Companion");
        if (companion == null)
            Debug.LogError("No GameObject with tag 'Companion' found in scene.");

        sceneLoader = FindFirstObjectByType<SceneLoader>();
        if (sceneLoader == null)
            Debug.LogError("SceneLoader not found in the scene. Please add one and assign it.");
    }

    private void Start()
    {
        // setup toggles: initialize state without firing callbacks, then add listeners
        if (playerToggle != null && player != null)
        {
            suppressToggleEvents = true;
            playerToggle.isOn = player.activeSelf;
            suppressToggleEvents = false;
            playerToggle.onValueChanged.AddListener(SetPlayerEnabled);
        }

        if (companionToggle != null && companion != null)
        {
            suppressToggleEvents = true;
            companionToggle.isOn = companion.activeSelf;
            suppressToggleEvents = false;
            companionToggle.onValueChanged.AddListener(SetCompanionEnabled);
        }

        if (directionalLightToggle != null)
        {
            // initialize based on current directional light state
            Light dirLight = RenderSettings.sun;

            // If RenderSettings.sun is missing, attempt to find any directional Light in the scene and assign it.
            if (dirLight == null)
            {
                Light[] allLights = FindObjectsOfType<Light>(true);
                foreach (var l in allLights)
                {
                    if (l != null && l.type == LightType.Directional)
                    {
                        dirLight = l;
                        RenderSettings.sun = dirLight;
                        Debug.Log($"Assigned RenderSettings.sun to existing directional light: '{dirLight.name}'.");
                        break;
                    }
                }
            }

            if (dirLight != null)
            {
                suppressToggleEvents = true;
                directionalLightToggle.isOn = dirLight.enabled;
                suppressToggleEvents = false;
                // Use the method that reads/writes RenderSettings.sun so we don't capture a stale local variable.
                directionalLightToggle.onValueChanged.AddListener(SetDirectionalLightEnabled);
            }
            else
            {
                Debug.LogWarning("No directional light (RenderSettings.sun) found in scene.");
                directionalLightToggle.interactable = false;
            }
        }

        // add button listeners separately (previous code incorrectly grouped them)
        if (spawnEnemyButton != null)
        {
            spawnEnemyButton.onClick.AddListener(SpawnEnemy);
            // set initial interactable state based on _enemy presence
            if (enemy != null)
                spawnEnemyButton.interactable = !enemy.activeSelf;
            else
                spawnEnemyButton.interactable = true;
        }

        if (removeEnemyButton != null)
        {
            removeEnemyButton.onClick.AddListener(RemoveEnemy);
        }

        if (mainLevelButton != null)
        {
            mainLevelButton.onClick.AddListener(() =>
            {
                sceneLoader.LoadSceneByName(SceneNames.GameScene);
                EndDebugMode();
            });
        }

        if (vFXLevelButton != null)
        {
            vFXLevelButton.onClick.AddListener(() =>
            {
                sceneLoader.LoadSceneByName(SceneNames.VFXLevel);
                EndDebugMode();
            });
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                sceneLoader.LoadSceneByName(SceneNames.MainMenu);
                EndDebugMode();
            });

        }
    }

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(ON_DEBUG_MODE_ON, StartDebugMode);
        EventBroadcaster.Instance.AddObserver(ON_DEBUG_MODE_OFF, EndDebugMode);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_DEBUG_MODE_ON, StartDebugMode);
        EventBroadcaster.Instance.RemoveActionAtObserver(ON_DEBUG_MODE_OFF, EndDebugMode);

        if (playerToggle != null)
            playerToggle.onValueChanged.RemoveListener(SetPlayerEnabled);
        if (companionToggle != null)
            companionToggle.onValueChanged.RemoveListener(SetCompanionEnabled);
        if (spawnEnemyButton != null)
            spawnEnemyButton.onClick.RemoveListener(SpawnEnemy);
        if (removeEnemyButton != null)
            removeEnemyButton.onClick.RemoveListener(RemoveEnemy);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.P))
        {
            checkpoint.ReturnToCheckpoint();
        }

        if (Input.GetKey(KeyCode.Q))
        {
            checkpoint.SaveCheckpoint();
        }
    }

    private void StartDebugMode()
    {
        Time.timeScale = 0.0000001f;
        if (debugPanel != null)
            debugPanel.SetActive(true);

        // save and show/unlock cursor so _player can interact with UI
        previousCursorVisible = Cursor.visible;
        previousLockState = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerToggle != null && player != null)
        {
            suppressToggleEvents = true;
            playerToggle.isOn = player.activeSelf;
            suppressToggleEvents = false;
        }
        if (companionToggle != null && companion != null)
        {
            suppressToggleEvents = true;
            companionToggle.isOn = companion.activeSelf;
            suppressToggleEvents = false;
        }

        if (spawnEnemyButton != null && enemy != null)
            spawnEnemyButton.interactable = !enemy.activeSelf;

        Debug.Log("Debug mode activated.");
    }

    private void EndDebugMode()
    {
        Time.timeScale = 1f;
        if (debugPanel != null)
            debugPanel.SetActive(false);

        // restore previous cursor state
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousLockState;

        Debug.Log("Debug mode deactivated.");
    }

    // Optional inspector-bound methods: use suppress flag to avoid double-calls
    public void ToggleEnablePlayer()
    {
        if (player == null || playerToggle == null) return;

        // flip _player, then update toggle without firing the listener
        player.SetActive(!player.activeSelf);
        suppressToggleEvents = true;
        playerToggle.isOn = player.activeSelf;
        suppressToggleEvents = false;
    }

    public void ToggleEnableCompanion()
    {
        if (companion == null || companionToggle == null) return;

        companion.SetActive(!companion.activeSelf);
        suppressToggleEvents = true;
        companionToggle.isOn = companion.activeSelf;
        suppressToggleEvents = false;
    }

    public void ToggleDirectionalLight()
    {
        Light dirLight = RenderSettings.sun;
        if (dirLight == null || directionalLightToggle == null) return;
        dirLight.enabled = !dirLight.enabled;
        suppressToggleEvents = true;
        directionalLightToggle.isOn = dirLight.enabled;
        suppressToggleEvents = false;
    }

    public void SetPlayerEnabled(bool enabled)
    {
        if (suppressToggleEvents) return;
        if (player != null)
            player.SetActive(enabled);

        // ensure toggle reflects actual state (defensive)
        if (playerToggle != null)
        {
            suppressToggleEvents = true;
            playerToggle.isOn = player != null && player.activeSelf;
            suppressToggleEvents = false;
        }
    }

    public void SetCompanionEnabled(bool enabled)
    {
        if (suppressToggleEvents) return;
        if (companion != null)
            companion.SetActive(enabled);

        if (companionToggle != null)
        {
            suppressToggleEvents = true;
            companionToggle.isOn = companion != null && companion.activeSelf;
            suppressToggleEvents = false;
        }
    }

    public void SetDirectionalLightEnabled(bool enabled)
    {
        if (suppressToggleEvents) return;
        Light dirLight = RenderSettings.sun;
        if (dirLight != null)
            dirLight.enabled = enabled;
        if (directionalLightToggle != null)
        {
            suppressToggleEvents = true;
            directionalLightToggle.isOn = dirLight != null && dirLight.enabled;
            suppressToggleEvents = false;
        }
    }
    public void SpawnEnemy()
    {
        if (enemy != null)
        {
            enemy.SetActive(true);
            if (spawnEnemyButton != null)
                spawnEnemyButton.interactable = false;
        }
    }

    public void RemoveEnemy()
    {
        if (enemy != null)
        {
            enemy.SetActive(false);
            if (spawnEnemyButton != null)
                spawnEnemyButton.interactable = true;
        }
    }
}