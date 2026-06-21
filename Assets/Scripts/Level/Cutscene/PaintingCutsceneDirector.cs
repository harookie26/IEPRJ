using System.Collections;
using Level.UI;
using UnityEngine;
using UnityEngine.UI;

public readonly struct PaintingCutsceneRequest
{
    public PaintingCutsceneRequest(
        Transform target,
        float startDistance,
        float endDistance,
        float heightOffset,
        float fadeDuration,
        float moveDuration,
        float viewDuration,
        DialoguePlaybackHandle dialoguePlayback = null)
    {
        Target = target;
        StartDistance = startDistance;
        EndDistance = endDistance;
        HeightOffset = heightOffset;
        FadeDuration = fadeDuration;
        MoveDuration = moveDuration;
        ViewDuration = viewDuration;
        DialoguePlayback = dialoguePlayback;
    }

    public Transform Target { get; }
    public float StartDistance { get; }
    public float EndDistance { get; }
    public float HeightOffset { get; }
    public float FadeDuration { get; }
    public float MoveDuration { get; }
    public float ViewDuration { get; }
    public DialoguePlaybackHandle DialoguePlayback { get; }
}

[DisallowMultipleComponent]
public class PaintingCutsceneDirector : MonoBehaviour
{
    public static PaintingCutsceneDirector Instance { get; private set; }

    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Image fadeImage;

    private Coroutine activeCutscene;
    private Camera gameplayCamera;
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

        if (enemyManager != null)
        {
            enemyManager.PauseEnemyForCutscene();
            Debug.Log("[PaintingCutsceneDirector] EnemyManager found. Pausing enemies for cutscene.");
        }
        else
        {
            Debug.LogWarning("[PaintingCutsceneDirector] No EnemyManager found. Enemies will not be paused during the cutscene.");
        }

        Vector3 paintingCenter = request.Target.position + new Vector3(0f, request.HeightOffset, 0f);

        gameplayCamera = Camera.main;
        Vector3 viewerPosition = gameplayCamera != null
            ? gameplayCamera.transform.position
            : paintingCenter + request.Target.forward;
        Vector3 outwardDirection = GetPaintingSurfaceDirection(request.Target, viewerPosition);

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

        while (request.DialoguePlayback != null && !request.DialoguePlayback.IsComplete)
            yield return null;

        yield return Fade(0f, 1f, request.FadeDuration);

        cutsceneCamera.enabled = false;
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        yield return Fade(1f, 0f, request.FadeDuration);

        if (enemyManager != null)
        {
            enemyManager.ResumeEnemyFromCutscene();
            Debug.Log("[PaintingCutsceneDirector] Resuming enemies after cutscene.");
        }
        else
        {             
            Debug.LogWarning("[PaintingCutsceneDirector] No EnemyManager found. Enemies will not be resumed after the cutscene."); 
        }

        gameplayCamera = null;
        activeCutscene = null;
        EndCutsceneState();
    }

    private static Vector3 GetPaintingSurfaceDirection(Transform painting, Vector3 viewerPosition)
    {
        MeshFilter meshFilter = painting.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = painting.GetComponentInChildren<MeshFilter>();

        Transform meshTransform = meshFilter != null ? meshFilter.transform : painting;
        Vector3 meshSize = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds.size
            : Vector3.one;
        Vector3 scale = meshTransform.lossyScale;
        meshSize = Vector3.Scale(meshSize, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        // A framed painting's thinnest mesh dimension is perpendicular to its
        // visible surface. Restricting the camera to this axis prevents edge-on
        // shots regardless of how the imported FBX is rotated.
        Vector3[] localAxes = { meshTransform.right, meshTransform.up, meshTransform.forward };
        float[] dimensions = { meshSize.x, meshSize.y, meshSize.z };
        Vector3 surfaceDirection = Vector3.zero;

        for (int selection = 0; selection < dimensions.Length; selection++)
        {
            int smallestIndex = 0;
            for (int i = 1; i < dimensions.Length; i++)
            {
                if (dimensions[i] < dimensions[smallestIndex])
                    smallestIndex = i;
            }

            Vector3 flatAxis = Vector3.ProjectOnPlane(localAxes[smallestIndex], Vector3.up);
            dimensions[smallestIndex] = float.PositiveInfinity;

            if (flatAxis.sqrMagnitude > 0.0001f)
            {
                surfaceDirection = flatAxis.normalized;
                break;
            }
        }

        if (surfaceDirection.sqrMagnitude < 0.0001f)
            surfaceDirection = Vector3.ProjectOnPlane(painting.forward, Vector3.up).normalized;
        if (surfaceDirection.sqrMagnitude < 0.0001f)
            surfaceDirection = Vector3.forward;

        Vector3 directionToViewer = Vector3.ProjectOnPlane(
            viewerPosition - painting.position,
            Vector3.up);
        if (directionToViewer.sqrMagnitude > 0.0001f &&
            Vector3.Dot(surfaceDirection, directionToViewer) < 0f)
        {
            surfaceDirection = -surfaceDirection;
        }

        return surfaceDirection;
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
}
