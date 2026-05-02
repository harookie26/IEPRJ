using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VFXManager : MonoBehaviour
{
    [Header("Vignette Settings")] [Tooltip("Minimum vignette intensity.")]
    [SerializeField] private float minVignetteIntensity = 0.1f;
    [Tooltip("Maximum vignette intensity.")]
    [SerializeField] private float maxVignetteIntensity = 0.6f;
    [Tooltip("Intensity change per step when calling increase/decrease.")]
    [SerializeField] private float vignetteStep = 0.05f;
    [Tooltip("Interpolation speed for smoothing vignette intensity changes.")]
    [SerializeField] private float vignetteLerpSpeed = 8f;
    [Tooltip("Optional explicit reference to a Global Volume containing a Vignette override.")]
    [SerializeField] private Volume globalVolume;

    private Vignette vignette;
    private float targetVignetteIntensity;

    void Start()
    {
        // If no volume assigned, attempt to find a global volume in the scene.
        if (globalVolume == null)
        {
            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i] != null && volumes[i].isGlobal)
                {
                    globalVolume = volumes[i];
                    break;
                }
            }
        }

        if (globalVolume != null)
        {
            if (globalVolume.profile == null)
            {
                globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            // Try get existing vignette override
            if (!globalVolume.profile.TryGet(out vignette))
            {
                // Add one if missing
                vignette = globalVolume.profile.Add<Vignette>(true);
            }

            if (vignette != null)
            {
                if (!vignette.intensity.overrideState) vignette.intensity.overrideState = true;
                targetVignetteIntensity = vignette.intensity.value; // initialize target
            }
        }
    }

    void Update()
    {
        if (vignette == null) return;

        // Smoothly interpolate toward target intensity
        float current = vignette.intensity.value;
        float t = Mathf.Clamp01(vignetteLerpSpeed * Time.deltaTime);
        float newVal = Mathf.Lerp(current, targetVignetteIntensity, t);
        vignette.intensity.value = newVal;

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            IncreaseVignette();
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            DecreaseVignette();
        }

    }

    // PUBLIC API -----------------------------------------------------------------
    public void IncreaseVignette()
    {
        if (vignette == null) return;
        targetVignetteIntensity = Mathf.Min(maxVignetteIntensity, targetVignetteIntensity + vignetteStep);
    }

    public void DecreaseVignette()
    {
        if (vignette == null) return;
        targetVignetteIntensity = Mathf.Max(minVignetteIntensity, targetVignetteIntensity - vignetteStep);
    }
}
