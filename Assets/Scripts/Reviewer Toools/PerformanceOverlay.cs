using TMPro;
using UnityEngine;

public class PerformanceOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private TextMeshProUGUI resolutionText;

    [Header("Behavior")]
    [SerializeField, Tooltip("How often (seconds) to update the text.")]
    private float updateInterval = 0.25f;
    [SerializeField, Tooltip("If true, shows ms/frame and FPS.")]
    private bool showMs = true;

    private float smoothedDeltaTime;
    private float nextUpdateTime;
    private bool isVisible = true;

    private void Awake()
    {
        // Optional convenience: if not wired in Inspector, try to find one in children.
        if (fpsText == null)
            fpsText = GetComponentInChildren<TextMeshProUGUI>(true);

        ApplyVisibility();
    }

    private void Start()
    {
        if (sceneNameText != null)
            sceneNameText.text = $"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
        if (resolutionText != null)
        {
            resolutionText.text = $"{Screen.width}x{Screen.height}";
        }
    }

    private void Update()
    {

        if (!isVisible || fpsText == null)
            return;

        // Smooth using unscaled time (ignores Time.timeScale)
        float dt = Time.unscaledDeltaTime;
        smoothedDeltaTime += (dt - smoothedDeltaTime) * 0.1f;

        // Update label at a fixed interval to reduce allocations
        if (Time.unscaledTime < nextUpdateTime)
            return;

        nextUpdateTime = Time.unscaledTime + Mathf.Max(0.05f, updateInterval);

        float fps = smoothedDeltaTime > 0f ? (1f / smoothedDeltaTime) : 0f;

        if (showMs)
        {
            float ms = smoothedDeltaTime * 1000f;
            fpsText.text = $"{fps:0.} FPS ({ms:0.0} ms)";
        }
        else
        {
            fpsText.text = $"{fps:0.} FPS";
        }
    }

    // For UI Button OnClick() hookup or other scripts.
    public void ToggleFPS()
    {
        isVisible = !isVisible;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (fpsText != null)
            fpsText.gameObject.SetActive(isVisible);

        if (sceneNameText != null)
            sceneNameText.gameObject.SetActive(isVisible);

        if (resolutionText != null)
            resolutionText.gameObject.SetActive(isVisible);
    }
}
