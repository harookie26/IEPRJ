using Game.ObjectTypes;
using TMPro; // Add this for the battery text
using UnityEngine;
using static EventNames.GameStateEvents;

public class Flashlight : MonoBehaviour
{
    private const float GhostDetectionRadius = 0.35f;
    private const float GhostDetectionAngleTolerance = 4f;
    private const int MaxGhostHits = 32;
    private static readonly Collider[] GhostColliderBuffer = new Collider[MaxGhostHits];

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

    private Light flashlightLight;

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
        if (flashlightBeam != null) flashlightLight = flashlightBeam.GetComponent<Light>();

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
        if (TryGetGhostInLight(out EnemyStateMachine ghost))
        {
            // Call the Freeze function with your custom duration
            ghost.Freeze(ghost.StunDuration);
            Debug.Log("Ghost is caught in light - Stun timer paused.");
        }
    }

    private bool TryGetGhostInLight(out EnemyStateMachine ghost)
    {
        ghost = null;

        Transform lightTransform = flashlightBeam != null ? flashlightBeam.transform : null;
        Transform sourceTransform = lightTransform != null ? lightTransform : camTransform;
        if (sourceTransform == null) return false;

        Vector3 origin = sourceTransform.position;
        Vector3 direction = sourceTransform.forward;
        float range = flashlightLight != null ? Mathf.Max(stunRange, flashlightLight.range) : stunRange;
        float halfAngle = flashlightLight != null ? flashlightLight.spotAngle * 0.5f : 28f;

        int colliderCount = Physics.OverlapSphereNonAlloc(
            origin,
            range,
            GhostColliderBuffer,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidate = GhostColliderBuffer[i];
            if (candidate == null) continue;

            Vector3 toCandidate = candidate.bounds.center - origin;
            if (toCandidate.sqrMagnitude <= GhostDetectionRadius * GhostDetectionRadius)
            {
                ghost = candidate.GetComponentInParent<EnemyStateMachine>();
                if (ghost != null) return true;
                continue;
            }

            float angle = Vector3.Angle(direction, toCandidate);
            if (angle <= halfAngle + GhostDetectionAngleTolerance)
            {
                ghost = candidate.GetComponentInParent<EnemyStateMachine>();
                if (ghost != null) return true;
            }
        }

        return camTransform != null
            && camTransform != sourceTransform
            && TryGetGhostFromDirection(camTransform.position, camTransform.forward, range, halfAngle, out ghost);
    }

    private bool TryGetGhostFromDirection(Vector3 origin, Vector3 direction, float range, float halfAngle, out EnemyStateMachine ghost)
    {
        ghost = null;

        int colliderCount = Physics.OverlapSphereNonAlloc(
            origin,
            range,
            GhostColliderBuffer,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidate = GhostColliderBuffer[i];
            if (candidate == null) continue;

            Vector3 toCandidate = candidate.bounds.center - origin;
            float angle = Vector3.Angle(direction, toCandidate);
            if (angle > halfAngle + GhostDetectionAngleTolerance) continue;

            ghost = candidate.GetComponentInParent<EnemyStateMachine>();
            if (ghost != null) return true;
        }

        return false;
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
