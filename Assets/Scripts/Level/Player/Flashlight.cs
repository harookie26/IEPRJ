using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EventNames.GameStateEvents;

public class Flashlight : MonoBehaviour
{
    private const float GhostDetectionAngleTolerance = 4f;
    private const int MaxGhostHits = 32;
    private const int MaxOcclusionHits = 32;
    private static readonly Collider[] GhostColliderBuffer = new Collider[MaxGhostHits];
    private static readonly RaycastHit[] OcclusionHitBuffer = new RaycastHit[MaxOcclusionHits];

    [Header("References")]
    [SerializeField] private GameObject flashlightObject;
    [SerializeField] private GameObject flashlightBeam;
    [SerializeField] private TextMeshProUGUI batteryText;
    [SerializeField] private Image batteryFill;
    [SerializeField] private Transform camTransform;

    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField, Range(0f, 100f)] private float initialPickupBatteryPercent = 2f;
    [SerializeField] private float drainRate = 2f; // Percent per second
    private float currentBattery;

    [Header("Stun Settings")]
    [SerializeField] private float stunRange = 10f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    [Header("Audio")]
    [SerializeField] private AudioClip flashlightAudioClip;
    private AudioSource sfxAudioSource;

    private PlayerCollectibleManager collectibles;

    private bool hasCollectedFlashlight = false;

    private bool canToggle = true;
    private bool allowToggleDuringCutscene = false;
    private bool allowBatteryDrainDuringCutscene = false;

    private bool isOn = false;
    private bool presentationSuppressed;
    private bool isBatteryDeathFlickering;
    private Coroutine batteryDeathRoutine;

    private bool hasLoadedData = false;
    private bool hasInitializedPickupBattery = false;

    private Light flashlightLight;

    private EnemyStateMachine activeFrozenGhost = null;
    public bool IsOn => isOn;
    public float BatteryPercent => currentBattery;
    public Transform HeldTransform => flashlightObject != null ? flashlightObject.transform : transform;

    public event Action TurnedOn;
    public event Action BatteryDeathStarted;
    public event Action BatteryDepleted;

    public void SetIsOn(bool value)
    {
        bool wasOn = isOn;
        isOn = value && currentBattery > 0f;
        UpdateBeamState();

        if (!wasOn && isOn)
            TurnedOn?.Invoke();
    }

    public void SetCutsceneToggleAllowed(bool value)
    {
        allowToggleDuringCutscene = value;
    }

    public void SetCutsceneBatteryDrainAllowed(bool value)
    {
        allowBatteryDrainDuringCutscene = value;
    }

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

        if (isBatteryDeathFlickering)
        {
            UpdateUI();
            return;
        }

        if (GameState.IsCutsceneActive)
        {
            if (allowToggleDuringCutscene)
                HandleInput();

            if (allowBatteryDrainDuringCutscene && isOn && currentBattery > 0f)
                DrainBattery();

            UpdateUI();
            return;
        }

        HandleInput();

        if (isOn && currentBattery > 0)
        {
            DrainBattery();
            CheckForGhost();
        }
        else
        {
            if (currentBattery <= 0 && isOn)
            {
                isOn = false;
                UpdateBeamState();
            }
            ReleaseFrozenGhost();
        }

        UpdateUI();
    }

    private void HandleInput()
    {
        if (collectibles == null)
            return;

        if (collectibles.HasCollected("Flashlight") && !hasCollectedFlashlight)
        {
            hasCollectedFlashlight = true;

            if (!hasLoadedData && !hasInitializedPickupBattery)
            {
                SetBatteryPercent(initialPickupBatteryPercent);
                hasInitializedPickupBattery = true;
            }

            if (batteryText != null)
                batteryText.gameObject.SetActive(true);

            if (flashlightObject != null && !presentationSuppressed)
                flashlightObject.SetActive(true);
        }

        if (Input.GetMouseButtonDown(0) && currentBattery > 0 && collectibles.HasCollected("Flashlight"))
        {
            if (flashlightAudioClip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(flashlightAudioClip);
            }
            SetIsOn(!isOn);
        }
    }

    private void DrainBattery()
    {
        float previousBattery = currentBattery;
        currentBattery -= drainRate * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);

        if (previousBattery > 0f && currentBattery <= 0f && batteryDeathRoutine == null)
        {
            BatteryDeathStarted?.Invoke();
            batteryDeathRoutine = StartCoroutine(PlayBatteryDeathFlicker());
        }
    }

    private IEnumerator PlayBatteryDeathFlicker()
    {
        isBatteryDeathFlickering = true;
        ReleaseFrozenGhost();

        float[] flickerDurations = { 0.14f, 0.08f, 0.2f, 0.07f, 0.31f, 0.09f, 0.42f, 0.07f, 0.18f };
        for (int i = 0; i < flickerDurations.Length; i++)
        {
            isOn = i % 2 != 0;
            UpdateBeamState();
            yield return new WaitForSeconds(flickerDurations[i]);
        }

        isOn = false;
        UpdateBeamState();
        yield return new WaitForSeconds(0.35f);
        isBatteryDeathFlickering = false;
        batteryDeathRoutine = null;
        BatteryDepleted?.Invoke();
    }

    public void RefillBattery(float percent = 100f)
    {
        SetBatteryPercent(percent);
    }

    public void SetBatteryPercent(float percent)
    {
        currentBattery = Mathf.Clamp(percent, 0f, maxBattery);

        if (currentBattery <= 0f && isOn)
        {
            isOn = false;
            UpdateBeamState();
            ReleaseFrozenGhost();
        }

        UpdateUI();
    }

    private void CheckForGhost()
    {
        if (TryGetGhostInLight(out EnemyStateMachine detectedGhost))
        {
            if (activeFrozenGhost != null && activeFrozenGhost != detectedGhost)
            {
                activeFrozenGhost.Unfreeze();
            }

            activeFrozenGhost = detectedGhost;
            activeFrozenGhost.Freeze(); 
            Debug.Log($"Ghost [{activeFrozenGhost.gameObject.name}] caught in light - Frozen.");
        }
        else
        {
            ReleaseFrozenGhost();
        }
    }

    private void ReleaseFrozenGhost()
    {
        if (activeFrozenGhost != null)
        {
            Debug.Log($"Flashlight contact lost with [{activeFrozenGhost.gameObject.name}] - Unfreezing.");
            activeFrozenGhost.Unfreeze();
            activeFrozenGhost = null;
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
        float range = flashlightLight != null ? Mathf.Min(stunRange, flashlightLight.range) : stunRange;
        float halfAngle = flashlightLight != null ? flashlightLight.spotAngle * 0.5f : 28f;

        if (TryGetGhostFromDirection(origin, direction, range, halfAngle, out ghost))
            return true;

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
            if (toCandidate.sqrMagnitude > range * range) continue;

            float angle = Vector3.Angle(direction, toCandidate);
            if (angle > halfAngle + GhostDetectionAngleTolerance) continue;

            EnemyStateMachine candidateGhost = candidate.GetComponentInParent<EnemyStateMachine>();
            if (candidateGhost == null || !HasLineOfSight(origin, candidate, candidateGhost)) continue;

            ghost = candidateGhost;
            return true;
        }

        return false;
    }

    private bool HasLineOfSight(Vector3 origin, Collider candidate, EnemyStateMachine candidateGhost)
    {
        Bounds bounds = candidate.bounds;
        Vector3 center = bounds.center;

        if (IsPointVisible(origin, center, candidateGhost)) return true;
        if (IsPointVisible(origin, center + Vector3.up * bounds.extents.y * 0.75f, candidateGhost)) return true;
        if (IsPointVisible(origin, center - Vector3.up * bounds.extents.y * 0.75f, candidateGhost)) return true;

        Vector3 horizontal = Vector3.Cross(Vector3.up, center - origin).normalized;
        if (horizontal.sqrMagnitude <= Mathf.Epsilon) horizontal = Vector3.right;

        float sideOffset = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.6f;
        return IsPointVisible(origin, center + horizontal * sideOffset, candidateGhost)
            || IsPointVisible(origin, center - horizontal * sideOffset, candidateGhost);
    }

    private bool IsPointVisible(Vector3 origin, Vector3 target, EnemyStateMachine candidateGhost)
    {
        Vector3 toTarget = target - origin;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Epsilon) return true;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            toTarget / distance,
            OcclusionHitBuffer,
            distance,
            occlusionMask,
            QueryTriggerInteraction.Ignore);

        RaycastHit nearestHit = default;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = OcclusionHitBuffer[i];
            if (hit.collider == null || IsPlayerCollider(hit.collider)) continue;
            if (hit.distance >= nearestDistance) continue;

            nearestHit = hit;
            nearestDistance = hit.distance;
        }

        if (nearestHit.collider == null) return true;
        return nearestHit.collider.GetComponentInParent<EnemyStateMachine>() == candidateGhost;
    }

    private bool IsPlayerCollider(Collider collider)
    {
        Transform playerRoot = transform.root;
        Transform colliderTransform = collider.transform;
        return colliderTransform == playerRoot || colliderTransform.IsChildOf(playerRoot);
    }

    private void UpdateBeamState()
    {
        if (flashlightBeam != null)
            flashlightBeam.SetActive(isOn);
    }

    public void SetPresentationVisible(bool visible)
    {
        presentationSuppressed = !visible;
        if (flashlightObject != null)
            flashlightObject.SetActive(visible && hasCollectedFlashlight);
    }

    private void UpdateUI()
    {
        if (batteryText != null)
        {
            batteryText.text = $"{Mathf.CeilToInt(currentBattery)}%";
        }

        if (batteryFill != null)
        {
            batteryFill.fillAmount = currentBattery / maxBattery;
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
        this.hasInitializedPickupBattery = true;

        UpdateBeamState();
        UpdateUI();
    }
}
