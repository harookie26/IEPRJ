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

        activeCutscene = StartCoroutine(PlayCutscene(request));
        return true;
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
        Vector3 outwardDirection = GetTrueOutwardDirection(request.Target);
        Vector3 paintingCenter = request.Target.position + new Vector3(0f, request.HeightOffset, 0f);

        Vector3 startPosition = paintingCenter + outwardDirection * request.StartDistance;
        Vector3 endPosition = paintingCenter + outwardDirection * request.EndDistance;

        gameplayCamera = Camera.main;
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

        yield return new WaitForSeconds(request.ViewDuration);

        while (request.DialoguePlayback != null && !request.DialoguePlayback.IsComplete)
            yield return null;

        yield return Fade(0f, 1f, request.FadeDuration);

        cutsceneCamera.enabled = false;
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        yield return Fade(1f, 0f, request.FadeDuration);

        gameplayCamera = null;
        activeCutscene = null;
    }

    private static Vector3 GetTrueOutwardDirection(Transform painting)
    {
        PaintbrushChanneller player = FindFirstObjectByType<PaintbrushChanneller>();
        Vector3 directionToPlayer;

        if (player != null)
        {
            directionToPlayer = player.transform.position - painting.position;
            directionToPlayer.y = 0f;
            directionToPlayer.Normalize();
        }
        else
        {
            directionToPlayer = new Vector3(
                painting.forward.x,
                0f,
                painting.forward.z).normalized;
        }

        Vector3[] axes =
        {
            painting.forward,
            -painting.forward,
            painting.up,
            -painting.up,
            painting.right,
            -painting.right
        };

        Vector3 bestAxis = painting.forward;
        float maxDot = -Mathf.Infinity;

        foreach (Vector3 axis in axes)
        {
            Vector3 flatAxis = new Vector3(axis.x, 0f, axis.z);
            if (flatAxis.sqrMagnitude < 0.01f)
                continue;

            flatAxis.Normalize();

            float dot = Vector3.Dot(flatAxis, directionToPlayer);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestAxis = flatAxis;
            }
        }

        return bestAxis.normalized;
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
