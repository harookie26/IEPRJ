using System.Collections;
using UnityEngine;
using static EventNames;

[DisallowMultipleComponent]
public sealed class BatteryRechargeCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Flashlight flashlight;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private UIManager uiManager;

    [Header("Motion")]
    [SerializeField] private Vector3 handlingPositionOffset = new(0f, -0.24f, 0.1f);
    [SerializeField] private Vector3 handlingRotationOffset = new(72f, -18f, 24f);
    [SerializeField] private Vector3 batteryPresentationOffset = new(-0.16f, -0.28f, 0.55f);
    [SerializeField] private Vector3 batteryInsertionOffset = new(-0.2f, -0.68f, 0.38f);
    [SerializeField] private Vector3 batteryPresentationRotation = new(78f, 8f, 18f);
    [SerializeField, Range(5f, 35f)] private float cameraDownAngle = 18f;
    [SerializeField, Min(0.05f)] private float cancelReturnDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float completionReturnDuration = 0.8f;

    [Header("Prototype Audio")]
    [SerializeField] private AudioClip compartmentReleaseClip;
    [SerializeField] private AudioClip batteryHandlingClip;
    [SerializeField] private AudioClip compartmentCloseClip;

    private Camera gameplayCamera;
    private PlayerCamera cameraMotion;
    private AudioSource audioSource;
    private Coroutine activeSequence;
    private bool cancelRequested;
    private bool completed;
    private bool ownsCutsceneState;

    public bool IsPlaying => activeSequence != null;

    private void Awake()
    {
        ResolveReferences();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        if (activeSequence != null)
            StopCoroutine(activeSequence);

        activeSequence = null;
        RestoreControl();
    }

    public bool TryBegin(BatteryComponent battery)
    {
        if (IsPlaying || battery == null || !battery.CanUse)
            return false;

        ResolveReferences();
        if (flashlight == null || playerMovement == null || gameplayCamera == null)
        {
            Debug.LogWarning("[BatteryRechargeCutscene] Missing flashlight, player, or gameplay camera.", this);
            return false;
        }

        cancelRequested = false;
        completed = false;
        activeSequence = StartCoroutine(PlayInteractiveRecharge(battery));
        return true;
    }

    public void Cancel()
    {
        if (IsPlaying && !completed)
        {
            cancelRequested = true;
            audioSource?.Stop();
        }
    }

    private IEnumerator PlayInteractiveRecharge(BatteryComponent battery)
    {
        BeginCutscene();
        uiManager?.HideAll();

        Transform cameraTransform = gameplayCamera.transform;
        Transform heldTransform = flashlight.HeldTransform;
        Transform batteryTransform = battery.transform;
        Collider[] batteryColliders = battery.GetComponentsInChildren<Collider>(true);
        bool[] batteryColliderStates = new bool[batteryColliders.Length];
        for (int i = 0; i < batteryColliders.Length; i++)
        {
            batteryColliderStates[i] = batteryColliders[i].enabled;
            batteryColliders[i].enabled = false;
        }

        Rigidbody batteryBody = battery.GetComponentInChildren<Rigidbody>(true);
        bool batteryWasKinematic = batteryBody != null && batteryBody.isKinematic;
        Vector3 batteryVelocity = batteryBody != null ? batteryBody.linearVelocity : Vector3.zero;
        Vector3 batteryAngularVelocity = batteryBody != null ? batteryBody.angularVelocity : Vector3.zero;
        if (batteryBody != null)
        {
            batteryBody.isKinematic = true;
            batteryBody.linearVelocity = Vector3.zero;
            batteryBody.angularVelocity = Vector3.zero;
        }

        Vector3 cameraStartPosition = cameraTransform.localPosition;
        Quaternion cameraStartRotation = cameraTransform.localRotation;
        Vector3 heldStartPosition = heldTransform.localPosition;
        Quaternion heldStartRotation = heldTransform.localRotation;
        Vector3 batteryStartPosition = batteryTransform.position;
        Quaternion batteryStartRotation = batteryTransform.rotation;
        Quaternion cameraHandlingRotation = cameraStartRotation * Quaternion.Euler(cameraDownAngle, 0f, 0f);
        Vector3 heldHandlingPosition = heldStartPosition + handlingPositionOffset;
        Quaternion heldHandlingRotation = heldStartRotation * Quaternion.Euler(handlingRotationOffset);
        bool flashlightWasOn = flashlight.IsOn;
        float duration = Mathf.Max(0.5f, battery.RequiredReplacementDuration);
        bool releasePlayed = false;
        bool handlingPlayed = false;

        flashlight.SetIsOn(false);

        float elapsed = 0f;
        while (elapsed < duration && !cancelRequested)
        {
            if (battery == null || !battery.CanUse)
            {
                cancelRequested = true;
                break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float approach = EaseInOutSine(Mathf.InverseLerp(0f, 0.28f, progress));

            cameraTransform.localPosition = cameraStartPosition;
            cameraTransform.localRotation = Quaternion.Slerp(cameraStartRotation, cameraHandlingRotation, approach);

            Vector3 handlingDrift = Vector3.zero;
            Quaternion handlingTwist = Quaternion.identity;
            if (progress > 0.28f)
            {
                float workProgress = Mathf.InverseLerp(0.28f, 1f, progress);
                float envelope = Mathf.Sin(workProgress * Mathf.PI);
                handlingDrift = new Vector3(
                    Mathf.Sin(workProgress * Mathf.PI * 4f) * 0.012f,
                    Mathf.Sin(workProgress * Mathf.PI * 2f) * 0.008f,
                    0f) * envelope;
                handlingTwist = Quaternion.Euler(0f, 0f, Mathf.Sin(workProgress * Mathf.PI * 3f) * 5f * envelope);
            }

            heldTransform.localPosition = Vector3.Lerp(heldStartPosition, heldHandlingPosition, approach) + handlingDrift;
            heldTransform.localRotation = Quaternion.Slerp(heldStartRotation, heldHandlingRotation, approach) * handlingTwist;

            float batteryPickup = EaseInOutSine(Mathf.InverseLerp(0.05f, 0.48f, progress));
            float batteryInsert = EaseInOutSine(Mathf.InverseLerp(0.55f, 0.92f, progress));
            Vector3 batteryPresentationPosition = cameraTransform.TransformPoint(batteryPresentationOffset);
            Vector3 batteryInsertionPosition = cameraTransform.TransformPoint(batteryInsertionOffset);
            Quaternion batteryPresentationWorldRotation = cameraTransform.rotation * Quaternion.Euler(batteryPresentationRotation);

            batteryTransform.SetPositionAndRotation(
                Vector3.Lerp(
                    Vector3.Lerp(batteryStartPosition, batteryPresentationPosition, batteryPickup),
                    batteryInsertionPosition,
                    batteryInsert),
                Quaternion.Slerp(
                    batteryStartRotation,
                    batteryPresentationWorldRotation * Quaternion.Euler(0f, 0f, 35f * batteryInsert),
                    batteryPickup));

            if (!releasePlayed && progress >= 0.3f)
            {
                releasePlayed = true;
                PlayPrototypeSound(compartmentReleaseClip, 0.78f, 0.75f);
            }

            if (!handlingPlayed && progress >= 0.62f)
            {
                handlingPlayed = true;
                PlayPrototypeSound(batteryHandlingClip, 0.68f, 0.35f);
            }

            yield return null;
        }

        if (cancelRequested)
        {
            yield return AnimateReturn(
                cameraTransform,
                heldTransform,
                cameraStartPosition,
                cameraStartRotation,
                heldStartPosition,
                heldStartRotation,
                batteryTransform,
                batteryStartPosition,
                batteryStartRotation,
                true,
                cancelReturnDuration);

            RestoreBatteryPhysics(
                batteryColliders,
                batteryColliderStates,
                batteryBody,
                batteryWasKinematic,
                batteryVelocity,
                batteryAngularVelocity);
            flashlight.SetIsOn(flashlightWasOn);
            FinishSequence();
            yield break;
        }

        completed = true;
        bool replacementSucceeded = battery != null && battery.TryUse(flashlight);
        if (replacementSucceeded)
        {
            PlayPrototypeSound(compartmentCloseClip, 0.92f, 0.9f);
            yield return PlayInsertionJolt(cameraTransform, heldTransform);
        }

        yield return AnimateReturn(
            cameraTransform,
            heldTransform,
            cameraStartPosition,
            cameraStartRotation,
            heldStartPosition,
            heldStartRotation,
            batteryTransform,
            batteryStartPosition,
            batteryStartRotation,
            battery != null && battery.gameObject.activeInHierarchy,
            completionReturnDuration);

        if (battery != null)
            battery.transform.SetPositionAndRotation(batteryStartPosition, batteryStartRotation);
        RestoreBatteryPhysics(
            batteryColliders,
            batteryColliderStates,
            batteryBody,
            batteryWasKinematic,
            batteryVelocity,
            batteryAngularVelocity);

        flashlight.SetIsOn(flashlightWasOn);
        FinishSequence();
    }

    private IEnumerator PlayInsertionJolt(Transform cameraTransform, Transform heldTransform)
    {
        Vector3 cameraPosition = cameraTransform.localPosition;
        Quaternion cameraRotation = cameraTransform.localRotation;
        Vector3 heldPosition = heldTransform.localPosition;
        Quaternion heldRotation = heldTransform.localRotation;
        const float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float kick = Mathf.Sin(t * Mathf.PI) * (1f - t);
            cameraTransform.localPosition = cameraPosition + Vector3.down * (0.018f * kick);
            cameraTransform.localRotation = cameraRotation * Quaternion.Euler(1.4f * kick, 0f, -0.8f * kick);
            heldTransform.localPosition = heldPosition + Vector3.back * (0.035f * kick);
            heldTransform.localRotation = heldRotation * Quaternion.Euler(0f, 0f, -7f * kick);
            yield return null;
        }
    }

    private static IEnumerator AnimateReturn(
        Transform cameraTransform,
        Transform heldTransform,
        Vector3 cameraEndPosition,
        Quaternion cameraEndRotation,
        Vector3 heldEndPosition,
        Quaternion heldEndRotation,
        Transform batteryTransform,
        Vector3 batteryEndPosition,
        Quaternion batteryEndRotation,
        bool returnBattery,
        float duration)
    {
        Vector3 cameraStartPosition = cameraTransform.localPosition;
        Quaternion cameraStartRotation = cameraTransform.localRotation;
        Vector3 heldStartPosition = heldTransform.localPosition;
        Quaternion heldStartRotation = heldTransform.localRotation;
        Vector3 batteryStartPosition = batteryTransform != null ? batteryTransform.position : Vector3.zero;
        Quaternion batteryStartRotation = batteryTransform != null ? batteryTransform.rotation : Quaternion.identity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutSine(Mathf.Clamp01(elapsed / duration));
            cameraTransform.localPosition = Vector3.Lerp(cameraStartPosition, cameraEndPosition, t);
            cameraTransform.localRotation = Quaternion.Slerp(cameraStartRotation, cameraEndRotation, t);
            heldTransform.localPosition = Vector3.Lerp(heldStartPosition, heldEndPosition, t);
            heldTransform.localRotation = Quaternion.Slerp(heldStartRotation, heldEndRotation, t);
            if (returnBattery && batteryTransform != null)
            {
                batteryTransform.position = Vector3.Lerp(batteryStartPosition, batteryEndPosition, t);
                batteryTransform.rotation = Quaternion.Slerp(batteryStartRotation, batteryEndRotation, t);
            }
            yield return null;
        }

        cameraTransform.localPosition = cameraEndPosition;
        cameraTransform.localRotation = cameraEndRotation;
        heldTransform.localPosition = heldEndPosition;
        heldTransform.localRotation = heldEndRotation;
        if (returnBattery && batteryTransform != null)
            batteryTransform.SetPositionAndRotation(batteryEndPosition, batteryEndRotation);
    }

    private void PlayPrototypeSound(AudioClip clip, float pitch, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }

    private static void RestoreBatteryPhysics(
        Collider[] colliders,
        bool[] colliderStates,
        Rigidbody body,
        bool wasKinematic,
        Vector3 velocity,
        Vector3 angularVelocity)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = colliderStates[i];
        }

        if (body == null)
            return;

        body.isKinematic = wasKinematic;
        if (!wasKinematic)
        {
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
        }
    }

    private void ResolveReferences()
    {
        flashlight ??= GetComponent<Flashlight>();
        playerMovement ??= FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        uiManager ??= FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        gameplayCamera = Camera.main;
        if (gameplayCamera != null)
            cameraMotion = gameplayCamera.GetComponent<PlayerCamera>();
    }

    private void BeginCutscene()
    {
        if (ownsCutsceneState)
            return;

        ownsCutsceneState = true;
        InputManager.Instance?.SetCutsceneChannelInputAllowed(true);
        if (GameState.BeginCutscene())
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_START);
        if (cameraMotion != null)
            cameraMotion.enabled = false;
        playerMovement.ResetVelocity();
        playerMovement.SetCanMove(false);
    }

    private void RestoreControl()
    {
        InputManager.Instance?.SetCutsceneChannelInputAllowed(false);
        if (audioSource != null)
            audioSource.pitch = 1f;
        if (cameraMotion != null)
            cameraMotion.enabled = true;
        playerMovement?.SetCanMove(true);

        if (!ownsCutsceneState)
            return;

        ownsCutsceneState = false;
        if (GameState.EndCutscene())
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_END);
    }

    private void FinishSequence()
    {
        RestoreControl();
        cancelRequested = false;
        completed = false;
        activeSequence = null;
    }

    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }
}
