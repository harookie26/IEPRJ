using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CorruptedTutorialCutscene : MonoBehaviour
{
    public static CorruptedTutorialCutscene Instance { get; private set; }

    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Camera mainCamera;

    [Header("UI Requirements")]
    [Tooltip("Assign a UI CanvasGroup attached to a black full-screen image for the fade effect.")]
    [SerializeField] private Image fadeImage;

    [Header("Dynamic Cutscene Settings")]
    [Tooltip("Place this transform strictly in front of the visible canvas. If omitted, a child named CutsceneCameraAnchor is used.")]
    [SerializeField] private List<Transform> cutsceneCameraAnchors;
    [Tooltip("Max distance from the painting for the camera to add zoom")]
    [SerializeField] private float paintingDistance = .5f;
    [Tooltip("Adjust this if the camera is too high or too low (e.g., 1.5 for eye level).")]
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float moveDuration = 4f;
    [SerializeField] private float viewDuration = 5f;


    // We make this a class variable so the Coroutine can access it later
    private PaintingChannelable[] _corruptedPaintings;

    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Optionally uncomment the line below if you want this to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Invoke(nameof(InitializeCutscene), 1f); // Delay to ensure all paintings are initialized
    }

    private void InitializeCutscene()
    {
        PaintingChannelable[] allPaintings = FindObjectsOfType<PaintingChannelable>();
        List<PaintingChannelable> corruptedList = new List<PaintingChannelable>(allPaintings.Length);

        foreach (PaintingChannelable painting in allPaintings)
        {
            if (painting.PaintingId == "paint1" || painting.PaintingId == "paint2" || painting.PaintingId == "paint3" || painting.PaintingId == "paint4")
            {
                corruptedList.Add(painting);
            }
        }

        // 2. Sort the list alphabetically by the PaintingId
        corruptedList = corruptedList.OrderBy(p => p.PaintingId).ToList();

        _corruptedPaintings = corruptedList.ToArray();
        Debug.Log($"Found {_corruptedPaintings.Length} corrupted paintings.");

    }

    public void StartCorruptedPaintingCutscene()
    {
        if (cutsceneCamera == null)
        {
            Debug.LogWarning("Cutscene camera is not assigned.");
            return;
        }

        if (fadeImage == null) // Updated to fadeImage
        {
            Debug.LogWarning("Fade Image is not assigned!");
            return;
        }

        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        // 1. Setup Phase
        if (mainCamera != null) mainCamera.enabled = false; // Turn OFF player camera
        cutsceneCamera.enabled = true;

        // Set initial alpha to 1 (fully black) via the Image's color
        Color startColor = fadeImage.color;
        startColor.a = 1f;
        fadeImage.color = startColor;

        // 2. Loop through every corrupted painting we found
        for (int i = 0; i < _corruptedPaintings.Length; i++)
        {
            // Safety check: ensure we mapped an anchor for this painting
            if (i >= cutsceneCameraAnchors.Count)
            {
                Debug.LogWarning($"Missing camera anchor for painting {i}. Ending sequence.");
                break;
            }

            Transform currentAnchor = cutsceneCameraAnchors[i];
            PaintingChannelable currentPainting = _corruptedPaintings[i];

            // Teleport camera to the anchor and apply the height offset
            Vector3 targetPosition = currentAnchor.position + (Vector3.up * heightOffset);
            cutsceneCamera.transform.position = targetPosition;

            // FIX: Look directly at the exact geometric center of the painting's mesh
            Renderer paintingRenderer = currentPainting.GetComponentInChildren<Renderer>();

            if (paintingRenderer != null)
            {
                // Look at the calculated true center of the 3D model
                cutsceneCamera.transform.LookAt(paintingRenderer.bounds.center);
            }
            else
            {
                // Fallback just in case the object has no mesh/renderer
                cutsceneCamera.transform.LookAt(currentPainting.transform.position);
            }

            // Fade In (Black -> Clear)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

            // Optional Polish: Slowly move the camera forward using moveDuration & paintingDistance
            // yield return StartCoroutine(SlowDollyZoom(targetPosition, currentPainting.transform.position));

            // Wait and look at the painting
            yield return new WaitForSeconds(viewDuration);

            // Fade Out (Clear -> Black) to prepare for the next teleport
            // (We skip this on the last loop so we can transition smoothly back to the player)
            if (i < _corruptedPaintings.Length - 1)
            {
                yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
            }
        }

        // 3. Cleanup & Return Control
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration)); // Final fade to black

        cutsceneCamera.enabled = false;                                // Turn OFF cutscene camera
        if (mainCamera != null) mainCamera.enabled = true; // Turn ON player camera

        // Fade back to clear so the player can see their main camera again
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        Debug.Log("Cutscene complete. Player control restored.");
    }

    // Helper Coroutine to handle the UI fading math
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;

        // Grab the current color so we don't accidentally change its RGB values (keep it black)
        Color fadeColor = fadeImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // Calculate the new alpha and assign it back to the color
            fadeColor.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadeImage.color = fadeColor;

            // yield return null tells Unity to pause here and resume next frame
            yield return null;
        }

        // Ensure it snaps perfectly to the target alpha at the end
        fadeColor.a = endAlpha;
        fadeImage.color = fadeColor;
    }
}