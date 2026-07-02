using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 

public class EffectsController : MonoBehaviour
{
    [Header("Volume Configuration")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Camera targetCamera;

    [Header("Target Animation Values")]
    [SerializeField] private float targetVignetteIntensity = 0.65f;
    [SerializeField] private float targetChromaticIntensity = 0.8f;
    [SerializeField] private float targetFieldOfView = 30f;

    [Header("Depth of Field Settings")]
    [Tooltip("The focus distance target when you want a forced near/far blur effect.")]
    [SerializeField] private float targetFocusDistance = 0.5f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionSpeed = 5f;

    private Vignette vignetteComponent;
    private ChromaticAberration chromaticAberration;
    private DepthOfField depthOfFieldComponent;

    private float defaultVignette = 0.2f;
    private float defaultChromatic = 0.0f;
    private float defaultFOV = 60f;
    private float defaultFocusDistance = 3f;

    private bool isAnimatingSurge = false;
    private bool isAnimatingFocusDistance = false;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            defaultFOV = targetCamera.fieldOfView;
        }

        if (postProcessVolume != null)
        {
            if (postProcessVolume.profile.TryGet(out vignetteComponent))
            {
                defaultVignette = vignetteComponent.intensity.value;
            }

            postProcessVolume.profile.TryGet(out chromaticAberration);

            if (postProcessVolume.profile.TryGet(out depthOfFieldComponent))
            {
                defaultFocusDistance = depthOfFieldComponent.focusDistance.value;
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] PostProcess Volume is unassigned!");
        }
    }

    private void Update()
    {
        float step = Time.deltaTime * transitionSpeed;

        float vignetteDest = isAnimatingSurge ? targetVignetteIntensity : defaultVignette;
        float chromaticDest = isAnimatingSurge ? targetChromaticIntensity : defaultChromatic;
        float fovDest = isAnimatingSurge ? targetFieldOfView : defaultFOV;

        if (vignetteComponent != null)
            vignetteComponent.intensity.value = Mathf.MoveTowards(vignetteComponent.intensity.value, vignetteDest, step);

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.MoveTowards(chromaticAberration.intensity.value, chromaticDest, step * 2f);

        if (targetCamera != null)
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, fovDest, step);


        // 2. Handle Focus Distance Lerping (controlled via separate animation states)
        if (depthOfFieldComponent != null)
        {
            float focusDest = isAnimatingFocusDistance ? targetFocusDistance : defaultFocusDistance;
            depthOfFieldComponent.focusDistance.value = Mathf.Lerp(depthOfFieldComponent.focusDistance.value, focusDest, step);
        }
    }

    // --- PUBLIC FUNCTIONS FOR ANIMATION EVENTS ---

    public void TriggerPostProcessSurge()
    {
        isAnimatingSurge = true;
    }

    public void ResetToDefaultPostProcess()
    {
        isAnimatingSurge = false;
    }

    public void TriggerFocusDistanceShift()
    {
        isAnimatingFocusDistance = true;
    }

    public void ResetFocusDistanceToDefault()
    {
        isAnimatingFocusDistance = false;
    }
}