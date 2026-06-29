using System.Collections;
using Game.States;
using Level.UI;
using UnityEngine;
using static EventNames;

[DisallowMultipleComponent]
public sealed class ElevatorAttackCutscene : MonoBehaviour, ISaveable
{
    [Header("Lounge Elevator Attack")]
    [SerializeField] private bool enableLoungeElevatorAttack = true;
    [SerializeField] private string loungeElevatorName = "Lounge to Hallway 1";
    [SerializeField] private string requiredCollectibleId = "Flashlight";
    [SerializeField] private GameObject ghostRevealPrefab;
    [SerializeField] private bool reducedMotion = false;

    [Header("Lounge Elevator Attack Timing")]
    [SerializeField, Min(0f)] private float suspenseDelay = 0.45f;
    [SerializeField, Min(0.05f)] private float yankDuration = 0.32f;
    [SerializeField, Min(0.05f)] private float fallDuration = 0.26f;
    [SerializeField, Min(0f)] private float lookBackDelay = 0.25f;
    [SerializeField, Min(0.1f)] private float lookBackDuration = 1.25f;
    [SerializeField, Min(0f)] private float revealHoldDuration = 0.75f;
    [SerializeField, Min(0.1f)] private float standUpDuration = 1.1f;
    [SerializeField, Min(0.01f)] private float recoveryFadeDuration = 0.22f;

    [Header("Lounge Elevator Attack Motion")]
    [SerializeField, Min(0.5f)] private float dragDistance = 3f;
    [SerializeField, Min(0.05f)] private float floorCameraHeight = 0.22f;
    [SerializeField, Range(0f, 60f)] private float impactRoll = 32f;
    [SerializeField, Range(20f, 140f)] private float landingLookOffset = 72f;
    [SerializeField, Min(0.25f)] private float ghostSpawnForwardDistance = 1.15f;
    [SerializeField, Min(0.1f)] private float ghostWalkSpeed = 0.7f;
    [SerializeField, Range(0.1f, 1f)] private float ghostWalkAnimationSpeed = 0.45f;
    [SerializeField, Min(0.5f)] private float ghostStopDistance = 1.2f;
    [SerializeField, Min(0f)] private float ghostGroundSink = 0.35f;
    [SerializeField, Min(0f)] private float ghostRevealLightIntensity = 15f;
    [SerializeField, Min(0.1f)] private float ghostRevealLightRange = 4f;

    [Header("Lounge Elevator Attack Flashlight Response")]
    [SerializeField] private string georgieFlashlightPrompt = "Use the flashlight!";
    [SerializeField, Min(0f)] private float flashlightTutorialDelay = 0.8f;
    [SerializeField, Min(0)] private int flashlightTutorialPanelIndex = 1;

    [Header("Lounge Elevator Attack Audio (Optional)")]
    [SerializeField] private AudioClip dragSfx;
    [SerializeField] private AudioClip impactSfx;
    [SerializeField] private AudioClip revealSfx;

    private bool _loungeAttackPlayed;
    private bool _loungeAttackInProgress;

    private GameObject _player;
    private PlayerMovement _playerMovement;
    private UIManager _uiManager;
    private AudioList _audioList;
    private AudioSource _audioSource;
    private Flashlight _flashlight;

    public bool HasPlayed => _loungeAttackPlayed;
    public string SaveKey => "LoungeElevatorAttack";

    private ScreenFader ScreenFader => FindFirstObjectByType<ScreenFader>();

    private void Awake()
    {
        GlobalSaveSystem.Register(this);
        ResolveDependencies();
    }

    private void OnDestroy()
    {
        GlobalSaveSystem.Unregister(this);
    }

    private void ResolveDependencies()
    {
        ResolvePlayerReferences();
        _uiManager ??= FindFirstObjectByType<UIManager>();
        _audioList ??= FindAnyObjectByType<AudioList>();
        _audioSource ??= GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _flashlight ??= _player != null
            ? _player.GetComponentInChildren<Flashlight>(true)
            : FindFirstObjectByType<Flashlight>(FindObjectsInactive.Include);
    }

    public bool CanPlay(StairsComponent door)
    {
        if (!enableLoungeElevatorAttack
            || _loungeAttackPlayed
            || _loungeAttackInProgress
            || door == null)
            return false;

        if (!Matches(door))
            return false;

        PlayerCollectibleManager collectibles = PlayerCollectibleManager.Instance;
        return collectibles != null && collectibles.HasCollected(requiredCollectibleId);
    }

    public bool Matches(StairsComponent door)
    {
        return enableLoungeElevatorAttack
            && door != null
            && string.Equals(
                door.gameObject.name,
                loungeElevatorName,
                System.StringComparison.Ordinal);
    }

    public IEnumerator Play(StairsComponent door)
    {
        _loungeAttackInProgress = true;
        _uiManager?.ClearForcedHUD();

        ResolveDependencies();
        Camera gameplayCamera = Camera.main;
        PlayerCamera cameraMotion = gameplayCamera != null
            ? gameplayCamera.GetComponent<PlayerCamera>()
            : null;

        if (_player == null || _playerMovement == null || gameplayCamera == null)
        {
            Debug.LogError("[ElevatorAttack] Missing player, PlayerMovement, or Main Camera. Falling back to the normal elevator transfer.");
            _loungeAttackInProgress = false;
            yield break;
        }

        bool beganGlobalCutscene = GameState.BeginCutscene();
        if (beganGlobalCutscene)
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_START);

        Vector3 savedCameraLocalPosition = gameplayCamera.transform.localPosition;
        Quaternion savedCameraLocalRotation = gameplayCamera.transform.localRotation;
        float savedFieldOfView = gameplayCamera.fieldOfView;
        bool savedCameraMotionEnabled = cameraMotion != null && cameraMotion.enabled;

        Vector3 playerStartPosition = _player.transform.position;
        Quaternion playerStartRotation = _player.transform.rotation;
        Vector3 cameraStartPosition = gameplayCamera.transform.position;
        Quaternion cameraStartRotation = gameplayCamera.transform.rotation;
        Vector3 viewForward = Vector3.ProjectOnPlane(
            gameplayCamera.transform.forward,
            Vector3.up).normalized;
        if (viewForward.sqrMagnitude < 0.001f)
            viewForward = Vector3.ProjectOnPlane(_player.transform.forward, Vector3.up).normalized;

        Vector3 elevatorDirection = Vector3.ProjectOnPlane(
            door.transform.position - playerStartPosition,
            Vector3.up).normalized;
        if (elevatorDirection.sqrMagnitude < 0.001f)
            elevatorDirection = viewForward;

        Vector3 dragDirection = -elevatorDirection;
        float safeDragDistance = CalculateSafeDragDistance(
            playerStartPosition,
            cameraStartPosition,
            dragDirection,
            dragDistance,
            _player.transform,
            door.transform);
        Vector3 safePlayerEnd = playerStartPosition + dragDirection * safeDragDistance;

        if (safeDragDistance < dragDistance - 0.01f)
        {
            Debug.Log(
                $"[ElevatorAttack] Drag distance clamped from {dragDistance:F2}m " +
                $"to {safeDragDistance:F2}m to avoid crossing level geometry.");
        }

        // The player begins directly in front of the elevator, so moving forward
        // from that interaction pose stages the ghost against the open doors.
        Vector3 ghostPosition = playerStartPosition
            + elevatorDirection * ghostSpawnForwardDistance;
        Vector3 ghostLookDirection = Vector3.ProjectOnPlane(safePlayerEnd - ghostPosition, Vector3.up);
        Quaternion ghostRotation = ghostLookDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(ghostLookDirection.normalized, Vector3.up)
            : Quaternion.LookRotation(-elevatorDirection, Vector3.up);
        Vector3 ghostFocus = ghostPosition + Vector3.up * 1.35f;

        EnemyStateMachine sceneEnemy = FindFirstObjectByType<EnemyStateMachine>(
            FindObjectsInactive.Include);
        GameObject ghostSource = sceneEnemy != null
            ? sceneEnemy.gameObject
            : ghostRevealPrefab;

        GameObject ghost = null;
        Animator ghostAnimator = null;
        if (ghostSource != null)
        {
            ghost = new GameObject("Elevator Attack Ghost");
            ghost.SetActive(false);

            GameObject ghostVisual = Instantiate(ghostSource, ghost.transform);
            ghostVisual.name = "Visual";

            DisableGhostGameplay(ghost);
            Renderer[] characterRenderers = ConfigureGhostRenderers(ghost);

            ghostAnimator = ghost.GetComponentInChildren<Animator>(true);
            if (ghostAnimator != null)
            {
                ghostAnimator.enabled = true;
                ghostAnimator.applyRootMotion = false;
                ghostAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                ghostAnimator.SetBool("isWalking", false);
                ghostAnimator.SetBool("isRunning", false);
                ghostAnimator.SetBool("isStunned", false);
            }

            // Reset prefab-authored scene placement. Preserve only non-upright
            // import-axis correction; ordinary saved yaw is replaced by the
            // authored reveal direction.
            Quaternion modelAxisCorrection = ghostVisual.transform.localRotation;
            bool usesStandardUpAxis = Vector3.Dot(
                modelAxisCorrection * Vector3.up,
                Vector3.up) > 0.9f;
            ghostVisual.transform.localPosition = Vector3.zero;
            ghostVisual.transform.localRotation = usesStandardUpAxis
                ? Quaternion.identity
                : modelAxisCorrection;

            ghost.transform.SetPositionAndRotation(
                ghostPosition,
                ghostRotation);

            if (TryAlignGhostToGround(
                ghost,
                characterRenderers,
                playerStartPosition.y - ghostGroundSink,
                out Bounds ghostBounds))
            {
                ghostFocus = ghostBounds.center
                    + Vector3.up * (ghostBounds.extents.y * 0.35f);
            }

            AddGhostRevealLight(ghost, ghostFocus, safePlayerEnd);
        }
        else
        {
            Debug.LogWarning("[ElevatorAttack] No ghost reveal prefab is assigned. The camera attack will still play.", this);
        }

        if (cameraMotion != null)
            cameraMotion.enabled = false;

        _playerMovement.ResetVelocity();
        _playerMovement.SetCanMove(false);

        bool sceneEnemyWasEnabled = sceneEnemy != null && sceneEnemy.enabled;
        bool frozenEnemy = sceneEnemy != null && sceneEnemy.isEnemyActivated;
        if (frozenEnemy)
            sceneEnemy.Freeze();

        // Disable the live scene enemy state machine for the full authored beat.
        // The reveal clone has already had all MonoBehaviours disabled above.
        if (sceneEnemy != null)
            sceneEnemy.enabled = false;

        if (_audioList != null && _audioList.elevatorSFX != null)
            _audioSource.PlayOneShot(_audioList.elevatorSFX);

        yield return StartCoroutine(door.PlayAnimationThenDeactivate());
        if (suspenseDelay > 0f)
            yield return new WaitForSeconds(suspenseDelay);

        Vector3 floorPosition = safePlayerEnd + Vector3.up * floorCameraHeight;
        Quaternion floorLookRotation = Quaternion.LookRotation(
            (ghostFocus - floorPosition).normalized,
            Vector3.up);

        if (reducedMotion)
        {
            if (ScreenFader != null)
                yield return StartCoroutine(ScreenFader.FadeOutSequence(0.08f));

            gameplayCamera.transform.SetPositionAndRotation(floorPosition, floorLookRotation);
            ghost?.SetActive(true);

            if (ScreenFader != null)
                yield return StartCoroutine(ScreenFader.FadeInSequence(0.12f));
        }
        else
        {
            if (dragSfx != null)
                _audioSource?.PlayOneShot(dragSfx);

            Vector3 yankPosition = cameraStartPosition
                + dragDirection * safeDragDistance
                + Vector3.down * 0.12f;
            Quaternion yankRotation = cameraStartRotation * Quaternion.Euler(-7f, 4f, impactRoll * 0.32f);
            yield return StartCoroutine(AnimateCameraPose(
                gameplayCamera.transform,
                cameraStartPosition,
                cameraStartRotation,
                yankPosition,
                yankRotation,
                yankDuration,
                CameraMotionPhase.Yank));

            Vector3 landingDirection = Quaternion.AngleAxis(
                landingLookOffset,
                Vector3.up) * elevatorDirection;
            Quaternion impactRotation = Quaternion.LookRotation(
                landingDirection,
                Vector3.up) * Quaternion.Euler(0f, 0f, -impactRoll);
            yield return StartCoroutine(AnimateCameraPose(
                gameplayCamera.transform,
                yankPosition,
                yankRotation,
                floorPosition,
                impactRotation,
                fallDuration,
                CameraMotionPhase.Fall));

            ghost?.SetActive(true);
            if (impactSfx != null)
                _audioSource?.PlayOneShot(impactSfx);
            yield return StartCoroutine(PlayImpactShake(gameplayCamera.transform, floorPosition, impactRotation));

            if (revealSfx != null)
                _audioSource?.PlayOneShot(revealSfx);

            if (lookBackDelay > 0f)
                yield return new WaitForSeconds(lookBackDelay);

            // Recover from wherever the impact left the view, then deliberately
            // turn back toward the elevator reveal.
            yield return StartCoroutine(AnimateCameraPose(
                gameplayCamera.transform,
                gameplayCamera.transform.position,
                gameplayCamera.transform.rotation,
                floorPosition,
                floorLookRotation * Quaternion.Euler(0f, 0f, -7f),
                lookBackDuration,
                CameraMotionPhase.Settle));
        }

        bool retryEncounter = false;
        if (ghost != null)
        {
            // Require a fresh press during the threat instead of accepting a
            // flashlight that happened to be on before the elevator opened.
            _flashlight?.SetIsOn(false);
            _flashlight?.SetCutsceneToggleAllowed(true);

            DialogueManager.Instance?.DisplayLatest(
                "Georgie",
                georgieFlashlightPrompt,
                0.08f,
                2.2f,
                0.18f);

            Coroutine tutorialPrompt = StartCoroutine(
                ShowFlashlightTutorialAfterDelay(flashlightTutorialDelay));

            bool flashlightRepelledGhost = false;
            Vector3 ghostFocusOffset = ghostFocus - ghost.transform.position;
            yield return StartCoroutine(AnimateGhostApproach(
                ghost,
                ghostAnimator,
                gameplayCamera.transform,
                floorPosition,
                ghostFocusOffset,
                safePlayerEnd,
                _flashlight,
                succeeded => flashlightRepelledGhost = succeeded));

            if (tutorialPrompt != null)
                StopCoroutine(tutorialPrompt);

            _flashlight?.SetCutsceneToggleAllowed(false);
            TutorialManager.Instance?.HideAllTutorials();
            retryEncounter = !flashlightRepelledGhost;
        }

        if (retryEncounter)
        {
            if (ScreenFader != null)
                yield return StartCoroutine(ScreenFader.FadeOutSequence(recoveryFadeDuration));

            if (ghost != null)
                Destroy(ghost);

            door.ResetAnimationPose();
            _playerMovement.TeleportToPose(playerStartPosition, playerStartRotation);
            _playerMovement.SetViewRotation(
                cameraStartRotation.eulerAngles.y,
                Mathf.DeltaAngle(0f, savedCameraLocalRotation.eulerAngles.x));
        }
        else
        {
            _loungeAttackPlayed = true;

            if (revealHoldDuration > 0f)
                yield return new WaitForSeconds(revealHoldDuration);

            if (ghost != null)
                Destroy(ghost);

            Quaternion playerEndRotation = Quaternion.LookRotation(
                elevatorDirection,
                Vector3.up);

            Vector3 cameraOffsetInPlayerSpace = Quaternion.Inverse(playerStartRotation)
                * (cameraStartPosition - playerStartPosition);
            Vector3 standingCameraPosition = safePlayerEnd
                + playerEndRotation * cameraOffsetInPlayerSpace;
            Quaternion standingCameraRotation = Quaternion.LookRotation(
                elevatorDirection,
                Vector3.up);

            if (reducedMotion)
            {
                door.ResetAnimationPose();
                gameplayCamera.transform.SetPositionAndRotation(
                    standingCameraPosition,
                    standingCameraRotation);
            }
            else
            {
                StartCoroutine(door.PlayResetAnimation(standUpDuration));
                yield return StartCoroutine(AnimateCameraPose(
                    gameplayCamera.transform,
                    gameplayCamera.transform.position,
                    gameplayCamera.transform.rotation,
                    standingCameraPosition,
                    standingCameraRotation,
                    standUpDuration,
                    CameraMotionPhase.Stand));
            }

            _playerMovement.TeleportToPose(safePlayerEnd, playerEndRotation);
            _playerMovement.SetViewRotation(playerEndRotation.eulerAngles.y, 0f);
        }

        gameplayCamera.transform.localPosition = savedCameraLocalPosition;
        gameplayCamera.transform.localRotation = savedCameraLocalRotation;
        gameplayCamera.fieldOfView = savedFieldOfView;
        if (cameraMotion != null)
            cameraMotion.enabled = savedCameraMotionEnabled;

        if (sceneEnemy != null)
        {
            sceneEnemy.enabled = sceneEnemyWasEnabled;
            if (frozenEnemy)
                sceneEnemy.Unfreeze();
        }

        _playerMovement.SetCanMove(true);

        if (GameState.EndCutscene())
            EventBroadcaster.Instance?.PostEvent(CutsceneEvents.CUTSCENE_END);

        if (retryEncounter && ScreenFader != null)
            yield return StartCoroutine(ScreenFader.FadeInSequence(recoveryFadeDuration));

        _loungeAttackInProgress = false;
    }

    public object CaptureState()
    {
        return new ElevatorAttackSaveData
        {
            hasPlayed = _loungeAttackPlayed
        };
    }

    public void RestoreState(object state)
    {
        _loungeAttackInProgress = false;
        _loungeAttackPlayed = state is ElevatorAttackSaveData data && data.hasPlayed;
    }

    private IEnumerator ShowFlashlightTutorialAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        TutorialManager.Instance?.TriggerTutorial(flashlightTutorialPanelIndex);
    }

    private enum CameraMotionPhase
    {
        Yank,
        Fall,
        Settle,
        Stand
    }

    private static float CalculateSafeDragDistance(
        Vector3 playerStartPosition,
        Vector3 cameraStartPosition,
        Vector3 dragDirection,
        float requestedDistance,
        Transform playerRoot,
        Transform doorRoot)
    {
        if (requestedDistance <= 0f || dragDirection.sqrMagnitude < 0.001f)
            return 0f;

        dragDirection.Normalize();
        float safeDistance = requestedDistance;

        // Sample the route at the camera, torso, and knees. This prevents both
        // the viewpoint and the player's body from being pulled through walls
        // or low furniture when the interaction begins from an unusual angle.
        safeDistance = ClampDragDistanceWithSphereCast(
            cameraStartPosition,
            0.16f,
            dragDirection,
            safeDistance,
            playerRoot,
            doorRoot);
        safeDistance = ClampDragDistanceWithSphereCast(
            playerStartPosition + Vector3.up * 0.85f,
            0.32f,
            dragDirection,
            safeDistance,
            playerRoot,
            doorRoot);
        safeDistance = ClampDragDistanceWithSphereCast(
            playerStartPosition + Vector3.up * 0.35f,
            0.25f,
            dragDirection,
            safeDistance,
            playerRoot,
            doorRoot);

        return safeDistance;
    }

    private static float ClampDragDistanceWithSphereCast(
        Vector3 origin,
        float radius,
        Vector3 direction,
        float currentDistance,
        Transform playerRoot,
        Transform doorRoot)
    {
        if (currentDistance <= 0f)
            return 0f;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            direction,
            currentDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        float nearestObstacle = currentDistance;
        bool foundObstacle = false;
        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
            if (hitTransform == null)
                continue;

            if (playerRoot != null && hitTransform.IsChildOf(playerRoot))
                continue;

            // The elevator itself may overlap the interaction pose. Only ignore
            // colliders belonging to that elevator, never its wall/room parents.
            if (doorRoot != null && hitTransform.IsChildOf(doorRoot))
                continue;

            nearestObstacle = Mathf.Min(nearestObstacle, hit.distance);
            foundObstacle = true;
        }

        if (!foundObstacle)
            return currentDistance;

        const float obstacleClearance = 0.08f;
        return Mathf.Max(0f, nearestObstacle - obstacleClearance);
    }

    private static IEnumerator AnimateCameraPose(
        Transform cameraTransform,
        Vector3 startPosition,
        Quaternion startRotation,
        Vector3 endPosition,
        Quaternion endRotation,
        float duration,
        CameraMotionPhase phase)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = phase switch
            {
                CameraMotionPhase.Fall => t * t,
                CameraMotionPhase.Stand => Mathf.SmoothStep(0f, 1f, t),
                _ => 1f - Mathf.Pow(1f - t, 4f)
            };

            Vector3 shake = Vector3.zero;
            if (phase == CameraMotionPhase.Yank)
            {
                float envelope = Mathf.Sin(t * Mathf.PI);
                shake = cameraTransform.right * (Mathf.Sin(t * 71f) * 0.035f * envelope)
                      + Vector3.up * (Mathf.Sin(t * 53f) * 0.025f * envelope);
            }

            cameraTransform.SetPositionAndRotation(
                Vector3.LerpUnclamped(startPosition, endPosition, eased) + shake,
                Quaternion.SlerpUnclamped(startRotation, endRotation, eased));
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(endPosition, endRotation);
    }

    private static IEnumerator PlayImpactShake(
        Transform cameraTransform,
        Vector3 position,
        Quaternion rotation)
    {
        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = 1f - t;
            float verticalKick = Mathf.Sin(t * Mathf.PI * 5f) * 0.09f * envelope;
            float rollKick = Mathf.Sin(t * Mathf.PI * 7f) * 7f * envelope;

            cameraTransform.SetPositionAndRotation(
                position + Vector3.up * verticalKick,
                rotation * Quaternion.Euler(0f, 0f, rollKick));
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(position, rotation);
    }

    private IEnumerator AnimateGhostApproach(
        GameObject ghost,
        Animator ghostAnimator,
        Transform cameraTransform,
        Vector3 cameraPosition,
        Vector3 focusOffset,
        Vector3 playerPosition,
        Flashlight flashlight,
        System.Action<bool> onComplete)
    {
        Vector3 startPosition = ghost.transform.position;
        Vector3 fromPlayer = Vector3.ProjectOnPlane(
            startPosition - playerPosition,
            Vector3.up);
        if (fromPlayer.sqrMagnitude < 0.001f)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        Vector3 endPosition = playerPosition
            + fromPlayer.normalized * ghostStopDistance;
        endPosition.y = startPosition.y;

        float distance = Vector3.Distance(startPosition, endPosition);
        float duration = distance / Mathf.Max(0.1f, ghostWalkSpeed);

        if (ghostAnimator != null)
        {
            const float authoredWalkSpeed = 0.7f;
            ghostAnimator.speed = Mathf.Clamp(
                ghostWalkAnimationSpeed * (ghostWalkSpeed / authoredWalkSpeed),
                0.1f,
                1f);
            ghostAnimator.SetBool("isWalking", true);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (flashlight != null && flashlight.IsOn)
            {
                ghost.SetActive(false);
                onComplete?.Invoke(true);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            ghost.transform.position = Vector3.LerpUnclamped(
                startPosition,
                endPosition,
                eased);

            Vector3 facePlayer = Vector3.ProjectOnPlane(
                playerPosition - ghost.transform.position,
                Vector3.up);
            if (facePlayer.sqrMagnitude > 0.001f)
            {
                ghost.transform.rotation = Quaternion.Slerp(
                    ghost.transform.rotation,
                    Quaternion.LookRotation(facePlayer.normalized, Vector3.up),
                    1f - Mathf.Exp(-5f * Time.deltaTime));
            }

            Vector3 focus = ghost.transform.position + focusOffset;
            Quaternion trackingRotation = Quaternion.LookRotation(
                (focus - cameraPosition).normalized,
                Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Lerp(-7f, -2f, t));
            cameraTransform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.Slerp(
                    cameraTransform.rotation,
                    trackingRotation,
                    1f - Mathf.Exp(-5f * Time.deltaTime)));

            yield return null;
        }

        ghost.transform.position = endPosition;
        if (ghostAnimator != null)
        {
            ghostAnimator.SetBool("isWalking", false);
            ghostAnimator.speed = 1f;
        }

        onComplete?.Invoke(false);
    }

    private static void DisableGhostGameplay(GameObject ghost)
    {
        // The cutscene clone is visual-only. Disable every gameplay script before
        // it is revealed so no AI/state machine can register or update.
        foreach (MonoBehaviour behaviour in ghost.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;

        foreach (UnityEngine.AI.NavMeshAgent agent in ghost.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            agent.enabled = false;

        foreach (AudioSource source in ghost.GetComponentsInChildren<AudioSource>(true))
            source.enabled = false;

        foreach (Collider ghostCollider in ghost.GetComponentsInChildren<Collider>(true))
            ghostCollider.enabled = false;

        foreach (Rigidbody ghostBody in ghost.GetComponentsInChildren<Rigidbody>(true))
            ghostBody.isKinematic = true;
    }

    private static Renderer[] ConfigureGhostRenderers(GameObject ghost)
    {
        Renderer[] allRenderers = ghost.GetComponentsInChildren<Renderer>(true);
        SkinnedMeshRenderer[] skinnedRenderers = ghost.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Renderer[] characterRenderers;

        if (skinnedRenderers.Length > 0)
        {
            characterRenderers = new Renderer[skinnedRenderers.Length];
            for (int i = 0; i < skinnedRenderers.Length; i++)
                characterRenderers[i] = skinnedRenderers[i];
        }
        else
        {
            // Fallback for a non-skinned replacement model. Explicitly reject
            // the enemy prefab's FOV/debug geometry.
            System.Collections.Generic.List<Renderer> visible = new();
            foreach (Renderer renderer in allRenderers)
            {
                string objectName = renderer.gameObject.name;
                bool isDebugGeometry = objectName.Contains("FOV")
                    || objectName.Contains("Range")
                    || objectName.StartsWith("Sphere");
                if (!isDebugGeometry)
                    visible.Add(renderer);
            }

            characterRenderers = visible.ToArray();
        }

        foreach (Renderer renderer in allRenderers)
            renderer.enabled = System.Array.IndexOf(characterRenderers, renderer) >= 0;

        foreach (Renderer ghostRenderer in characterRenderers)
        {
            ghostRenderer.enabled = true;

            Transform current = ghostRenderer.transform;
            while (current != null && current != ghost.transform)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }
        }

        return characterRenderers;
    }

    private static bool TryAlignGhostToGround(
        GameObject ghost,
        Renderer[] renderers,
        float groundHeight,
        out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer ghostRenderer in renderers)
        {
            if (!ghostRenderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = ghostRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(ghostRenderer.bounds);
            }
        }

        if (!hasBounds)
            return false;

        if (TryGetLowestFootHeight(ghost, out float footHeight))
        {
            ghost.transform.position += Vector3.up * (groundHeight - footHeight);
        }
        else
        {
            ghost.transform.position += Vector3.up * (groundHeight - bounds.min.y);
        }

        // Bounds are world-space, so recompute after shifting the root.
        hasBounds = false;
        foreach (Renderer ghostRenderer in renderers)
        {
            if (!ghostRenderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = ghostRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(ghostRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool TryGetLowestFootHeight(GameObject ghost, out float footHeight)
    {
        footHeight = float.PositiveInfinity;
        bool foundToe = false;

        Transform[] bones = ghost.GetComponentsInChildren<Transform>(true);
        foreach (Transform bone in bones)
        {
            if (bone.name.EndsWith("LeftToeBase", System.StringComparison.Ordinal)
                || bone.name.EndsWith("RightToeBase", System.StringComparison.Ordinal))
            {
                footHeight = Mathf.Min(footHeight, bone.position.y);
                foundToe = true;
            }
        }

        if (foundToe)
            return true;

        foreach (Transform bone in bones)
        {
            if (bone.name.EndsWith("LeftFoot", System.StringComparison.Ordinal)
                || bone.name.EndsWith("RightFoot", System.StringComparison.Ordinal))
            {
                footHeight = Mathf.Min(footHeight, bone.position.y);
            }
        }

        return !float.IsPositiveInfinity(footHeight);
    }

    private void AddGhostRevealLight(
        GameObject ghost,
        Vector3 ghostFocus,
        Vector3 viewerPosition)
    {
        if (ghostRevealLightIntensity <= 0f)
            return;

        GameObject lightObject = new GameObject("Ghost Reveal Light");
        lightObject.transform.SetParent(ghost.transform, true);

        Vector3 towardViewer = Vector3.ProjectOnPlane(
            viewerPosition - ghostFocus,
            Vector3.up).normalized;
        lightObject.transform.position = ghostFocus + towardViewer * 0.7f + Vector3.up * 0.25f;

        Light revealLight = lightObject.AddComponent<Light>();
        revealLight.type = LightType.Point;
        revealLight.color = new Color(0.63f, 0.72f, 0.78f);
        revealLight.intensity = ghostRevealLightIntensity;
        revealLight.range = ghostRevealLightRange;
        revealLight.shadows = LightShadows.None;
    }

    private void ResolvePlayerReferences()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player");

        if (_playerMovement == null && _player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            _playerMovement ??= _player.GetComponentInChildren<PlayerMovement>(true);
            _playerMovement ??= _player.GetComponentInParent<PlayerMovement>(true);
        }

        _playerMovement ??= FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (_player == null && _playerMovement != null)
            _player = _playerMovement.gameObject;
    }
}
