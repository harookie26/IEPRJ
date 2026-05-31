using Game.ObjectTypes;
using TMPro; // Add this for the battery text
using UnityEngine;
using static EventNames.GameStateEvents;

public class Flashlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject flashlightObject; // The actual flashlight model
    [SerializeField] private GameObject flashlightBeam;
    [SerializeField] private TextMeshProUGUI batteryText; // Assign a UI Text element here
    [SerializeField] private Transform camTransform; // Assign the main camera or flashlight tip

    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 2f; // Percent per second
    private float currentBattery;

    [Header("Stun Settings")]
    [SerializeField] private float stunRange = 10f;
    [SerializeField] private LayerMask enemyLayer; // Set this to the layer your Ghost is on

    [Header("Audio")]
    [SerializeField] private AudioClip flashlightAudioClip;
    private AudioSource sfxAudioSource;

    private PlayerCollectibleManager collectibles;

    private bool hasCollectedFlashlight = false;

    private bool canToggle = true;

    private bool isOn = false;

    private bool hasLoadedData = false;

    public void SetIsOn(bool value) => isOn = value;

    void Start()
    {
        EventBroadcaster.Instance.AddObserver(ON_GAME_PAUSE, GamePaused);
        EventBroadcaster.Instance.AddObserver(ON_GAME_RESUME, GameResumed);

        if (!hasLoadedData)
        {
            currentBattery = maxBattery;
        }

        if (camTransform == null) camTransform = Camera.main.transform;

        UpdateBeamState();

        sfxAudioSource = GetComponent<AudioSource>();

        collectibles = FindFirstObjectByType<PlayerCollectibleManager>();

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (EventBroadcaster.Instance != null)
        {
            EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_PAUSE, GamePaused);
            EventBroadcaster.Instance.RemoveActionAtObserver(ON_GAME_RESUME, GameResumed);
        }
    }

    private void GamePaused() => canToggle = false;

    private void GameResumed() => canToggle = true;

    void Update()
    {
        if (!canToggle) return;

        HandleInput();

        if (isOn && currentBattery > 0)
        {
            DrainBattery();
            CheckForGhost();
        }
        else if (currentBattery <= 0 && isOn)
        {
            isOn = false;
            UpdateBeamState();
        }

        UpdateUI();
    }

    private void HandleInput()
    {
        if(collectibles.HasCollected("Flashlight") && !hasCollectedFlashlight)
        {
            hasCollectedFlashlight = true;
            batteryText.gameObject.SetActive(true);
            flashlightObject.SetActive(true);
        }

        if (Input.GetMouseButtonDown(0) && currentBattery > 0 && collectibles.HasCollected("Flashlight"))
        {
            if (flashlightAudioClip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(flashlightAudioClip);
            }
            isOn = !isOn;
            UpdateBeamState();
        }
    }

    private void DrainBattery()
    {
        currentBattery -= drainRate * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
    }

    private void CheckForGhost()
    {
        int layerMask = enemyLayer | (1 << LayerMask.NameToLayer("Default"));

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, stunRange, enemyLayer))
        {
            // Use GetComponentInParent in case the collider is on a child object
            var ghost = hit.collider.GetComponentInParent<EnemyStateMachine>();

            if (ghost != null)
            {
                // Call the Freeze function with your custom duration
                ghost.Freeze(ghost.StunDuration);
                Debug.Log("Ghost is caught in light - Stun timer paused.");
            }
        }
    }

    private void UpdateBeamState()
    {
        if (flashlightBeam != null)
            flashlightBeam.SetActive(isOn);
    }

    private void UpdateUI()
    {
        if (batteryText != null)
        {
            batteryText.text = $"Battery: {Mathf.CeilToInt(currentBattery)}%";
        }
    }


    public FlashlightSaveData GetSaveData()
    {
        return new FlashlightSaveData
        {
            currentBattery = this.currentBattery,
            isOn = this.isOn
        };
    }


    public void LoadSaveData(FlashlightSaveData data)
    {
        if (data == null) return;

        this.currentBattery = data.currentBattery;
        this.isOn = data.isOn;

        // Mark that we have successfully loaded data
        this.hasLoadedData = true;

        UpdateBeamState();
        UpdateUI();
    }
}