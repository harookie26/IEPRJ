using System.Collections;
using UnityEngine;
using static EventNames;

[DisallowMultipleComponent]
public sealed class FlashlightOnboardingDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BatteryComponent onboardingBattery;
    [SerializeField] private Flashlight flashlight;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private UIManager uiManager;

    [Header("Pickup")]
    [SerializeField, Min(0.05f)] private float pickupLookDuration = 1.15f;
    [SerializeField, Min(0f)] private float pickupInspectHoldDuration = 0.55f;
    [SerializeField, Min(0.05f)] private float pickupMoveDuration = 1.8f;
    [SerializeField, Min(0f)] private float pickupCameraFollowSpeed = 4f;
    [SerializeField, Min(0.05f)] private float pickupSettleDuration = 0.9f;
    [SerializeField, Min(0f)] private float tutorialRevealDelay = 0.55f;
    [SerializeField] private int flashlightTutorialPanelIndex = 1;

    [Header("First Use Room Survey")]
    [SerializeField] private Vector2[] roomSurveyAngles =
    {
        new(-42f, -8f),
        new(-88f, 10f),
        new(-142f, -2f),
        new(168f, -10f),
        new(112f, 12f),
        new(58f, -14f),
        new(8f, 6f)
    };
    [SerializeField, Min(0f)] private float activationBreathDuration = 0.65f;
    [SerializeField, Min(1f)] private float surveyDegreesPerSecond = 28f;
    [SerializeField, Min(0.05f)] private float minimumSurveyMoveDuration = 0.8f;
    [SerializeField, Min(0f)] private float surveyPointHoldDuration = 0.45f;

    [Header("Battery Reveal")]
    [SerializeField, Min(0f)] private float batteryRevealDelay = 0.65f;
    [SerializeField, Min(0f)] private float batteryGlowHoldDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float batteryLookDuration = 1.65f;
    [SerializeField, Min(0f)] private float batteryFocusHoldDuration = 1.1f;

    private Camera gameplayCamera;
    private PlayerCamera cameraMotion;
    private Coroutine activeSequence;
    private bool flashlightActivated;
    private bool batteryDeathStarted;
    private bool batteryDepleted;
    private bool ownsCutsceneState;

    private void Awake()
    {
        onboardingBattery ??= GetComponent<BatteryComponent>();
        ResolveReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeEvents();
        StartCoroutine(ResumeDepletedSaveIfNeeded());
    }

    private IEnumerator ResumeDepletedSaveIfNeeded()
    {
        yield return new WaitForEndOfFrame();

        PlayerCollectibleManager collectibles = PlayerCollectibleManager.Instance;
        if (activeSequence == null
            && flashlight != null
            && onboardingBattery != null
            && collectibles != null
            && collectibles.HasCollected("Flashlight")
            && flashlight.BatteryPercent <= 0f
            && !onboardingBattery.CanUse)
        {
            activeSequence = StartCoroutine(PlayBatteryRevealSequence());
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        RestoreControl();
    }

    public bool TryBeginPickup(BaseCollectible collectible)
    {
        if (collectible == null || activeSequence != null)
            return false;

        ResolveReferences();
        SubscribeEvents();
        if (flashlight == null || gameplayCamera == null || playerMovement == null)
        {
            Debug.LogWarning("[FlashlightOnboarding] Missing flashlight, camera, or player movement; using the normal pickup flow.", this);
            return false;
        }

        activeSequence = StartCoroutine(PlayPickupSequence(collectible));
        return true;
    }

    private IEnumerator PlayPickupSequence(BaseCollectible collectible)
    {
        BeginCutscene();
        uiManager?.HideAll();

        Transform cameraTransform = gameplayCamera.transform;
        Vector3 savedLocalPosition = cameraTransform.localPosition;
        Quaternion forwardLocalRotation = Quaternion.identity;
        Transform pickupTransform = collectible.transform;
        Collider[] pickupColliders = collectible.GetComponentsInChildren<Collider>();
        foreach (Collider pickupCollider in pickupColliders)
            pickupCollider.enabled = false;
        collectible.enabled = false;

        flashlight.SetPresentationVisible(false);

        Quaternion pickupLookRotation = LookRotation(cameraTransform.position, pickupTransform.position);
        yield return AnimateWorldRotation(cameraTransform, cameraTransform.rotation, pickupLookRotation, pickupLookDuration);
        if (pickupInspectHoldDuration > 0f)
            yield return new WaitForSeconds(pickupInspectHoldDuration);

        Vector3 itemStartPosition = pickupTransform.position;
        Quaternion itemStartRotation = pickupTransform.rotation;
        Vector3 itemStartScale = pickupTransform.lossyScale;
        Transform heldTransform = flashlight.HeldTransform;
        Vector3 itemEndPosition = heldTransform.position;
        Quaternion itemEndRotation = heldTransform.rotation;
        Vector3 itemEndScale = heldTransform.lossyScale;

        float elapsed = 0f;
        while (elapsed < pickupMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / pickupMoveDuration));
            pickupTransform.SetPositionAndRotation(
                Vector3.Lerp(itemStartPosition, itemEndPosition, t),
                Quaternion.Slerp(itemStartRotation, itemEndRotation, t));
            SetWorldScale(pickupTransform, Vector3.Lerp(itemStartScale, itemEndScale, t));

            Quaternion followRotation = LookRotation(cameraTransform.position, pickupTransform.position);
            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                followRotation,
                1f - Mathf.Exp(-pickupCameraFollowSpeed * Time.deltaTime));
            yield return null;
        }

        collectible.gameObject.SetActive(false);
        flashlight.SetPresentationVisible(true);
        yield return AnimateLocalPose(cameraTransform, savedLocalPosition, forwardLocalRotation, pickupSettleDuration);
        playerMovement.SetViewRotation(playerMovement.transform.eulerAngles.y, 0f);
        RestoreCamera(cameraTransform, savedLocalPosition, forwardLocalRotation);
        if (tutorialRevealDelay > 0f)
            yield return new WaitForSeconds(tutorialRevealDelay);

        flashlightActivated = flashlight.IsOn;
        flashlight.SetCutsceneToggleAllowed(true);
        TutorialManager.Instance?.ShowPersistentTutorial(flashlightTutorialPanelIndex);

        while (!flashlightActivated)
            yield return null;

        TutorialManager.Instance?.HideTutorial(flashlightTutorialPanelIndex);
        flashlight.SetCutsceneToggleAllowed(false);
        flashlight.SetCutsceneBatteryDrainAllowed(true);
        batteryDeathStarted = flashlight.BatteryPercent <= 0f;
        batteryDepleted = flashlight.BatteryPercent <= 0f;
        if (activationBreathDuration > 0f)
            yield return new WaitForSeconds(activationBreathDuration);
        yield return PlayRoomSurveyUntilBatteryDies(cameraTransform, savedLocalPosition, forwardLocalRotation);

        flashlight.SetCutsceneBatteryDrainAllowed(false);
        while (!batteryDepleted)
            yield return null;
        yield return PlayBatteryRevealSequence();
    }

    private IEnumerator PlayRoomSurveyUntilBatteryDies(Transform cameraTransform, Vector3 localPosition, Quaternion centerRotation)
    {
        if (roomSurveyAngles == null || roomSurveyAngles.Length == 0)
        {
            while (!batteryDeathStarted)
                yield return null;
            yield break;
        }

        int surveyIndex = 0;
        while (!batteryDeathStarted)
        {
            Vector2 surveyAngle = roomSurveyAngles[surveyIndex];
            Quaternion targetRotation = centerRotation * Quaternion.Euler(surveyAngle.y, surveyAngle.x, 0f);
            float angularDistance = Quaternion.Angle(cameraTransform.localRotation, targetRotation);
            float moveDuration = Mathf.Max(
                minimumSurveyMoveDuration,
                angularDistance / Mathf.Max(1f, surveyDegreesPerSecond));

            yield return AnimateLocalPoseUntilBatteryDies(
                cameraTransform,
                localPosition,
                targetRotation,
                moveDuration);
            yield return WaitForBatteryOrDuration(surveyPointHoldDuration);
            surveyIndex = (surveyIndex + 1) % roomSurveyAngles.Length;
        }
    }

    private void HandleFlashlightTurnedOn()
    {
        flashlightActivated = true;
    }

    private void HandleBatteryDepleted()
    {
        batteryDepleted = true;
        if (activeSequence == null && onboardingBattery != null && onboardingBattery.CanUse == false)
            activeSequence = StartCoroutine(PlayBatteryRevealSequence());
    }

    private void HandleBatteryDeathStarted()
    {
        batteryDeathStarted = true;
    }

    private IEnumerator PlayBatteryRevealSequence()
    {
        ResolveReferences();
        if (gameplayCamera == null || playerMovement == null || onboardingBattery == null)
        {
            onboardingBattery?.RevealForOnboarding();
            activeSequence = null;
            yield break;
        }

        BeginCutscene();
        if (batteryRevealDelay > 0f)
            yield return new WaitForSeconds(batteryRevealDelay);
        onboardingBattery.RevealForOnboarding();
        if (batteryGlowHoldDuration > 0f)
            yield return new WaitForSeconds(batteryGlowHoldDuration);

        Transform cameraTransform = gameplayCamera.transform;
        Vector3 savedLocalPosition = cameraTransform.localPosition;
        Quaternion savedLocalRotation = cameraTransform.localRotation;
        Quaternion targetRotation = LookRotation(cameraTransform.position, onboardingBattery.transform.position);
        yield return AnimateWorldRotation(cameraTransform, cameraTransform.rotation, targetRotation, batteryLookDuration);
        while (!IsBatteryInView())
            yield return null;
        if (batteryFocusHoldDuration > 0f)
            yield return new WaitForSeconds(batteryFocusHoldDuration);

        float yaw = targetRotation.eulerAngles.y;
        float pitch = Mathf.DeltaAngle(0f, targetRotation.eulerAngles.x);
        playerMovement.SetViewRotation(yaw, pitch);
        RestoreCamera(cameraTransform, savedLocalPosition, Quaternion.Euler(pitch, 0f, 0f));
        EndCutscene();

        onboardingBattery.EnableInteractionForOnboarding();
        uiManager?.ShowHUDForce(UIManager.Keys.Recharge);
        activeSequence = null;
    }

    private bool IsBatteryInView()
    {
        if (gameplayCamera == null || onboardingBattery == null)
            return false;

        Vector3 viewport = gameplayCamera.WorldToViewportPoint(onboardingBattery.transform.position);
        return viewport.z > 0f
            && viewport.x >= 0.2f && viewport.x <= 0.8f
            && viewport.y >= 0.2f && viewport.y <= 0.8f;
    }

    private void HandleBatteryUsed(BatteryComponent usedBattery)
    {
        if (usedBattery == onboardingBattery)
            uiManager?.ClearForcedHUD();
    }

    private void ResolveReferences()
    {
        gameplayCamera = Camera.main;
        if (gameplayCamera != null)
            cameraMotion = gameplayCamera.GetComponent<PlayerCamera>();
        flashlight ??= FindFirstObjectByType<Flashlight>(FindObjectsInactive.Include);
        playerMovement ??= FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        uiManager ??= FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
    }

    private void SubscribeEvents()
    {
        if (flashlight != null)
        {
            flashlight.TurnedOn -= HandleFlashlightTurnedOn;
            flashlight.BatteryDeathStarted -= HandleBatteryDeathStarted;
            flashlight.BatteryDepleted -= HandleBatteryDepleted;
            flashlight.TurnedOn += HandleFlashlightTurnedOn;
            flashlight.BatteryDeathStarted += HandleBatteryDeathStarted;
            flashlight.BatteryDepleted += HandleBatteryDepleted;
        }

        if (onboardingBattery != null)
        {
            onboardingBattery.Used -= HandleBatteryUsed;
            onboardingBattery.Used += HandleBatteryUsed;
        }
    }

    private void UnsubscribeEvents()
    {
        if (flashlight != null)
        {
            flashlight.TurnedOn -= HandleFlashlightTurnedOn;
            flashlight.BatteryDeathStarted -= HandleBatteryDeathStarted;
            flashlight.BatteryDepleted -= HandleBatteryDepleted;
        }
        if (onboardingBattery != null)
            onboardingBattery.Used -= HandleBatteryUsed;
    }

    private void BeginCutscene()
    {
        if (ownsCutsceneState)
            return;

        ownsCutsceneState = true;
        if (GameState.BeginCutscene())
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_START);
        if (cameraMotion != null)
            cameraMotion.enabled = false;
        playerMovement?.ResetVelocity();
        playerMovement?.SetCanMove(false);
    }

    private void EndCutscene()
    {
        if (!ownsCutsceneState)
            return;

        ownsCutsceneState = false;
        if (cameraMotion != null)
            cameraMotion.enabled = true;
        playerMovement?.SetCanMove(true);
        if (GameState.EndCutscene())
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_END);
    }

    private void RestoreControl()
    {
        flashlight?.SetCutsceneToggleAllowed(false);
        flashlight?.SetCutsceneBatteryDrainAllowed(false);
        EndCutscene();
    }

    private static Quaternion LookRotation(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity;
    }

    private static IEnumerator AnimateWorldRotation(Transform target, Quaternion start, Quaternion end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            target.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
        target.rotation = end;
    }

    private static IEnumerator AnimateLocalPose(Transform target, Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 startPosition = target.localPosition;
        Quaternion startRotation = target.localRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            target.localPosition = Vector3.Lerp(startPosition, position, t);
            target.localRotation = Quaternion.Slerp(startRotation, rotation, t);
            yield return null;
        }
        RestoreCamera(target, position, rotation);
    }

    private IEnumerator AnimateLocalPoseUntilBatteryDies(Transform target, Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 startPosition = target.localPosition;
        Quaternion startRotation = target.localRotation;
        float elapsed = 0f;
        while (elapsed < duration && !batteryDeathStarted)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            target.localPosition = Vector3.Lerp(startPosition, position, t);
            target.localRotation = Quaternion.Slerp(startRotation, rotation, t);
            yield return null;
        }

        if (!batteryDeathStarted)
            RestoreCamera(target, position, rotation);
    }

    private IEnumerator WaitForBatteryOrDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !batteryDeathStarted)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private static void RestoreCamera(Transform target, Vector3 localPosition, Quaternion localRotation)
    {
        target.localPosition = localPosition;
        target.localRotation = localRotation;
    }

    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            parentScale.x != 0f ? worldScale.x / parentScale.x : worldScale.x,
            parentScale.y != 0f ? worldScale.y / parentScale.y : worldScale.y,
            parentScale.z != 0f ? worldScale.z / parentScale.z : worldScale.z);
    }
}
