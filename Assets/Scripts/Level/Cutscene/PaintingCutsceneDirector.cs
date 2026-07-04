using System.Collections;
using Level.UI;
using UnityEngine;
using UnityEngine.UI;

public readonly struct PaintingCutsceneRequest
{
    public PaintingCutsceneRequest(
        Transform target,
        Transform cameraAnchor,
        float startDistance,
        float endDistance,
        float heightOffset,
        float fadeDuration,
        float moveDuration,
        float viewDuration,
        DialoguePlaybackHandle dialoguePlayback = null,
        // --- NEW PROGRESS PARAMETERS ---
        Transform progressCameraAnchor = null,
        float progressViewDuration = 0f)
    {
        Target = target;
        CameraAnchor = cameraAnchor;
        StartDistance = startDistance;
        EndDistance = endDistance;
        HeightOffset = heightOffset;
        FadeDuration = fadeDuration;
        MoveDuration = moveDuration;
        ViewDuration = viewDuration;
        DialoguePlayback = dialoguePlayback;
        ProgressCameraAnchor = progressCameraAnchor;
        ProgressViewDuration = progressViewDuration;
    }

    public Transform Target { get; }
    public Transform CameraAnchor { get; }
    public float StartDistance { get; }
    public float EndDistance { get; }
    public float HeightOffset { get; }
    public float FadeDuration { get; }
    public float MoveDuration { get; }
    public float ViewDuration { get; }
    public DialoguePlaybackHandle DialoguePlayback { get; }

    public Transform ProgressCameraAnchor { get; }
    public float ProgressViewDuration { get; }
}

[DisallowMultipleComponent]
public class PaintingCutsceneDirector : MonoBehaviour
{
    public static PaintingCutsceneDirector Instance { get; private set; }

    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Image fadeImage;

    private Coroutine activeCutscene;
    private Camera gameplayCamera;
    private EnemyManager pausedEnemyManager;
    private bool ownsCutsceneState;

    public bool IsPlaying => activeCutscene != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PaintingCutsceneDirector] Multiple directors found. Using the first active instance.", this);
            return;
        }

        Instance = this;
        ResolveDependencies();
    }

    private void OnDestroy()
    {
        ResumeEnemy();
        EndCutsceneState();

        if (Instance == this)
            Instance = null;
    }

    public bool TryPlay(PaintingCutsceneRequest request)
    {
        if (IsPlaying)
        {
            Debug.LogWarning("[PaintingCutsceneDirector] A painting cutscene is already playing. Ignoring the new request.", this);
            return false;
        }

        if (request.Target == null)
        {
            Debug.LogWarning("[PaintingCutsceneDirector] Cannot play a cutscene without a painting target.", this);
            return false;
        }

        if (request.CameraAnchor == null)
        {
            Debug.LogError(
                $"[PaintingCutsceneDirector] '{request.Target.name}' has no CutsceneCameraAnchor. " +
                "The cutscene was skipped because front-facing placement cannot be guaranteed.",
                request.Target);
            return false;
        }

        ResolveDependencies();
        if (cutsceneCamera == null || fadeImage == null)
        {
            Debug.LogWarning("[PaintingCutsceneDirector] Cutscene dependencies not found. Skipping cutscene.", this);
            return false;
        }

        BeginCutsceneState();
        activeCutscene = StartCoroutine(PlayCutscene(request));
        return true;
    }

    private void BeginCutsceneState()
    {
        if (ownsCutsceneState) return;

        ownsCutsceneState = true;
        if (GameState.BeginCutscene())
            EventBroadcaster.Instance?.PostEvent(EventNames.CutsceneEvents.CUTSCENE_START);
    }

    private void EndCutsceneState()
    {
        if (!ownsCutsceneState) return;

        ownsCutsceneState = false;
        if (GameState.EndCutscene())
            EventBroadcaster.Instance?.PostEvent(EventNames.CutsceneEvents.CUTSCENE_END);
    }

    private void ResolveDependencies()
    {
        if (cutsceneCamera == null)
        {
            cutsceneCamera = GetComponent<Camera>();

            if (cutsceneCamera == null)
            {
                GameObject cameraObject = GameObject.FindWithTag("CutsceneCamera");
                if (cameraObject != null)
                    cutsceneCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (fadeImage == null)
        {
            GameObject fadeObject = GameObject.FindWithTag("FadeImage");
            if (fadeObject != null)
                fadeImage = fadeObject.GetComponent<Image>();
        }
    }

    private IEnumerator PlayCutscene(PaintingCutsceneRequest request)
    {
        EnemyManager enemyManager = FindFirstObjectByType<EnemyManager>();

        Vector3 paintingCenter = request.Target.position + new Vector3(0f, request.HeightOffset, 0f);

        gameplayCamera = Camera.main;
        Vector3 outwardDirection = Vector3.ProjectOnPlane(
            request.CameraAnchor.position - paintingCenter,
            Vector3.up);
        if (outwardDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogError(
                $"[PaintingCutsceneDirector] '{request.CameraAnchor.name}' must be positioned in front of '{request.Target.name}', not at its center.",
                request.CameraAnchor);
            activeCutscene = null;
            EndCutsceneState();
            yield break;
        }
        outwardDirection.Normalize();

        if (enemyManager != null)
        {
            pausedEnemyManager = enemyManager;
            pausedEnemyManager.PauseEnemyForCutscene();
            Debug.Log("[PaintingCutsceneDirector] EnemyManager found. Pausing enemies for cutscene.");
        }
        else
        {
            Debug.LogWarning("[PaintingCutsceneDirector] No EnemyManager found. Enemies will not be paused during the cutscene.");
        }

        Vector3 startPosition = paintingCenter + outwardDirection * request.StartDistance;
        Vector3 endPosition = paintingCenter + outwardDirection * request.EndDistance;

        if (gameplayCamera != null)
            gameplayCamera.enabled = false;

        cutsceneCamera.transform.position = startPosition;
        cutsceneCamera.transform.LookAt(paintingCenter);
        cutsceneCamera.enabled = true;

        yield return Fade(1f, 0f, request.FadeDuration);

        float elapsed = 0f;
        while (elapsed < request.MoveDuration)
        {
            cutsceneCamera.transform.position =
                Vector3.Lerp(startPosition, endPosition, elapsed / request.MoveDuration);
            cutsceneCamera.transform.LookAt(paintingCenter);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cutsceneCamera.transform.position = endPosition;
        cutsceneCamera.transform.LookAt(paintingCenter);

        yield return new WaitForSeconds(request.ViewDuration);

        if (request.ProgressCameraAnchor != null && request.ProgressViewDuration > 0f)
        {
            // 1. Fade to black
            yield return Fade(0f, 1f, request.FadeDuration);

            // 2. Snap the camera instantly to the new anchor's position and rotation
            cutsceneCamera.transform.SetPositionAndRotation(
                request.ProgressCameraAnchor.position,
                request.ProgressCameraAnchor.rotation
            );

            // 3. Fade back in
            yield return Fade(1f, 0f, request.FadeDuration);

            // Wait and look at the progress
            yield return new WaitForSeconds(request.ProgressViewDuration);

            while (request.DialoguePlayback != null && !request.DialoguePlayback.IsComplete)
                yield return null;

            // Fade back to black
            yield return Fade(0f, 1f, request.FadeDuration);
        }
        else
        {
            while (request.DialoguePlayback != null && !request.DialoguePlayback.IsComplete)
                yield return null;

            yield return Fade(0f, 1f, request.FadeDuration);
        }

        // Return control to the player
        cutsceneCamera.enabled = false;
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        // Fade the gameplay camera back in
        yield return Fade(1f, 0f, request.FadeDuration);

        ResumeEnemy();

        gameplayCamera = null;
        activeCutscene = null;
        EndCutsceneState();
    }

    private void ResumeEnemy()
    {
        if (pausedEnemyManager == null) return;

        pausedEnemyManager.ResumeEnemyFromCutscene();
        pausedEnemyManager = null;
        Debug.Log("[PaintingCutsceneDirector] Resuming enemies after cutscene.");
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            fadeImage.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }

    private IEnumerator PanToAnchor(Transform anchor, float duration)
    {
        Vector3 startPosition = cutsceneCamera.transform.position;
        Quaternion startRotation = cutsceneCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cutsceneCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, anchor.position, t),
                Quaternion.Slerp(startRotation, anchor.rotation, t));
            yield return null;
        }

        cutsceneCamera.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
    }
}
