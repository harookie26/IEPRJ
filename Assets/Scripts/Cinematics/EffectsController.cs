using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 

public class EffectsController : MonoBehaviour
{
    [Header("Volume Configuration")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Settings")]
    public float minVignetteIntensity = 0f;
    public float maxVignetteIntensity = 0.6f;
    public float maxDesaturationTarget = -50f;
    public float maxChromaticIntensity = 1f;
    public float zoomedFieldOfView = 50f;

    private Camera playerCamera;
    private float defaultFieldOfView = 60f;
    private bool isEffectActive = false;

    private Vignette vignetteComponent;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            defaultFieldOfView = playerCamera.fieldOfView;
        }
        else
        {
            Debug.LogWarning("[EffectsController] Player Camera is unassigned and Camera.main was not found!");
        }

        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignetteComponent);
            postProcessVolume.profile.TryGet(out colorAdjustments);
            postProcessVolume.profile.TryGet(out chromaticAberration);
        }
        else
        {
            Debug.LogWarning("[EffectsController] Volume component or Profile is missing from the scene!");
        }

        ResetEffects();
    }

    private void Update()
    {
        if (isEffectActive)
        {
            ApplyProximityEffects();
        }
    }
    public void TriggerEffectsOn()
    {
        isEffectActive = true;
    }
    public void TriggerEffectsOff()
    {
        isEffectActive = false;
        ResetEffects();
    }

    private void ApplyProximityEffects()
    {
        float proximityFactor = 0.5f; 

        if (vignetteComponent != null)
        {
            vignetteComponent.intensity.overrideState = true;
            vignetteComponent.intensity.value = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, proximityFactor);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = Mathf.Lerp(0f, maxDesaturationTarget, proximityFactor);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            float jitterAmount = Mathf.Sin(Time.time * 35f) * 0.15f * proximityFactor;
            float targetChromatic = Mathf.Lerp(0f, maxChromaticIntensity, proximityFactor) + jitterAmount;
            chromaticAberration.intensity.value = Mathf.Clamp01(targetChromatic);
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(defaultFieldOfView, zoomedFieldOfView, proximityFactor);
        }
    }

    public void ResetEffects()
    {
        if (vignetteComponent != null)
        {
            vignetteComponent.intensity.overrideState = true;
            vignetteComponent.intensity.value = minVignetteIntensity;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 0f;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0f;
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = defaultFieldOfView;
        }
    }
}