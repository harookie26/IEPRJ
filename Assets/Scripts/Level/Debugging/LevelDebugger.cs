using UnityEngine;
using UnityEngine.UI;
using static EventNames.GameStateEvents;

public class LevelDebugger : MonoBehaviour
{
    [SerializeField] private GameObject debugPanel;

    [SerializeField] private Toggle playerToggle;
    [SerializeField] private Toggle companionToggle;

    [SerializeField] private Button spawnEnemyButton;
    [SerializeField] private Button removeEnemyButton;

    private GameObject player;
    private GameObject enemy;
    private GameObject companion;

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
    }

    private void Start()
    {
        if (playerToggle != null && player != null)
        {
            playerToggle.isOn = player.activeSelf;
            playerToggle.onValueChanged.AddListener(SetPlayerEnabled);
        }

        if (companionToggle != null && companion != null)
        {
            companionToggle.isOn = companion.activeSelf;
            companionToggle.onValueChanged.AddListener(SetCompanionEnabled);
        }

        if (spawnEnemyButton != null && enemy != null)
        {
            spawnEnemyButton.onClick.AddListener(SpawnEnemy);
            removeEnemyButton.onClick.AddListener(RemoveEnemy);
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

    private void StartDebugMode()
    {
        Time.timeScale = 0.0000001f;
        debugPanel.SetActive(true);

        if (playerToggle != null && player != null)
            playerToggle.isOn = player.activeSelf;
        if (companionToggle != null && companion != null)
            companionToggle.isOn = companion.activeSelf;
        if (spawnEnemyButton != null && enemy != null)
            spawnEnemyButton.interactable = !enemy.activeSelf;

        Debug.Log("Debug mode activated.");
    }

    private void EndDebugMode()
    {
        Time.timeScale = 1f;
        debugPanel.SetActive(false);

        Debug.Log("Debug mode deactivated.");
    }

    public void ToggleEnablePlayer()
    {
        if (player != null)
            player.SetActive(!player.activeSelf);

        if (playerToggle != null && player != null)
            playerToggle.isOn = player.activeSelf;
    }

    public void ToggleEnableCompanion()
    {
        if (companion != null)
            companion.SetActive(!companion.activeSelf);

        if (companionToggle != null && companion != null)
            companionToggle.isOn = companion.activeSelf;
    }

    public void SetPlayerEnabled(bool enabled)
    {
        if (player != null)
            player.SetActive(enabled);
    }

    public void SetCompanionEnabled(bool enabled)
    {
        if (companion != null)
            companion.SetActive(enabled);
    }

    public void SpawnEnemy()
    {
        if (enemy != null)
            enemy.SetActive(true);
    }

    public void RemoveEnemy()
    {
        if (enemy != null)
            enemy.SetActive(false);
    }
}