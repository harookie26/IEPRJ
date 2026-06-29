using Game.States;
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[FoldableInspector]
public class BatteryComponent : MonoBehaviour, ISaveable
{
    [Header("Battery")]
    [SerializeField] private string batteryId;
    [SerializeField, Min(0.1f)] private float requiredReplacementDuration = 2f;
    [SerializeField, Range(0f, 100f)] private float refillPercent = 100f;
    [SerializeField] private bool consumeOnUse = true;

    [Header("Onboarding")]
    [SerializeField, Tooltip("Keeps this battery non-interactable and dark until the flashlight onboarding reveals it.")]
    private bool waitForFlashlightDepletion;

    [Header("Highlight / Glow Settings")]
    [SerializeField, Tooltip("Should this battery pulse its highlight/glow effect?")]
    private bool enableHighlight = true;

    [SerializeField, Tooltip("How fast the highlight pulses (cycles per second).")]
    [Range(0.1f, 5f)]
    private float highlightFrequency = 1.5f;

    [SerializeField, Tooltip("Minimum emission brightness.")]
    [Range(0f, 1f)]
    private float minHighlightIntensity = 0.2f;

    [SerializeField, Tooltip("Maximum emission brightness.")]
    [Range(0f, 5f)]
    private float maxHighlightIntensity = 2f;

    [SerializeField, Tooltip("The material property name used for emission.")]
    private string highlightPropertyName = "_EmissionColor";

    private bool isUsed;
    private bool onboardingRevealed;
    private bool onboardingInteractionEnabled;
    private float highlightTime;
    private readonly List<Material> runtimeMaterials = new();
    private readonly List<Color> originalEmissionColors = new();
    private readonly List<string> emissionPropertyNames = new();

    public string SaveKey => string.IsNullOrWhiteSpace(batteryId) ? GetHierarchyPath() : batteryId;
    public float RequiredReplacementDuration => requiredReplacementDuration;
    public bool CanUse => isActiveAndEnabled
        && !isUsed
        && (!waitForFlashlightDepletion || onboardingInteractionEnabled);
    public event Action<BatteryComponent> Used;

    private void Awake()
    {
        if (enableHighlight)
            SetupHighlightRenderer();

        GlobalSaveSystem.Register(this);
        ApplyUsedState();
        ApplyHighlightVisibility();
    }

    private void Update()
    {
        if (!enableHighlight || (waitForFlashlightDepletion && !onboardingRevealed))
            return;

        highlightTime += Time.deltaTime;
        ApplyHighlightPulse();
    }

    private void OnDestroy()
    {
        GlobalSaveSystem.Unregister(this);

        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
                Destroy(material);
        }
    }

    private void SetupHighlightRenderer()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            foreach (Material material in meshRenderer.materials)
            {
                material.EnableKeyword("_EMISSION");

                string propertyName = GetEmissionPropertyName(material);
                if (string.IsNullOrEmpty(propertyName))
                    continue;

                Color originalColor = material.GetColor(propertyName);
                if (originalColor == Color.black)
                    originalColor = Color.white;

                runtimeMaterials.Add(material);
                originalEmissionColors.Add(originalColor);
                emissionPropertyNames.Add(propertyName);
            }
        }
    }

    private string GetEmissionPropertyName(Material material)
    {
        if (material.HasProperty(highlightPropertyName))
            return highlightPropertyName;

        string alternateName = highlightPropertyName.StartsWith("_")
            ? highlightPropertyName.Replace("_", string.Empty)
            : "_" + highlightPropertyName;

        return material.HasProperty(alternateName) ? alternateName : string.Empty;
    }

    private void ApplyHighlightPulse()
    {
        float rawSin = Mathf.Sin(highlightTime * highlightFrequency * Mathf.PI * 2f);
        float normalizedValue = (rawSin + 1f) / 2f;
        float currentIntensity = Mathf.Lerp(
            minHighlightIntensity,
            maxHighlightIntensity,
            normalizedValue);

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material material = runtimeMaterials[i];
            if (material != null)
            {
                material.SetColor(
                    emissionPropertyNames[i],
                    originalEmissionColors[i] * currentIntensity);
            }
        }
    }

    public void RevealForOnboarding()
    {
        onboardingRevealed = true;
        highlightTime = 0f;
        ApplyHighlightVisibility();
    }

    public void EnableInteractionForOnboarding()
    {
        onboardingRevealed = true;
        onboardingInteractionEnabled = true;
        ApplyHighlightVisibility();
    }

    private void ApplyHighlightVisibility()
    {
        if (!enableHighlight)
            return;

        bool visible = !waitForFlashlightDepletion || onboardingRevealed;
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material material = runtimeMaterials[i];
            if (material != null)
                material.SetColor(emissionPropertyNames[i], visible ? originalEmissionColors[i] * minHighlightIntensity : Color.black);
        }
    }

    public bool TryUse(Flashlight flashlight)
    {
        if (!CanUse || flashlight == null)
            return false;

        flashlight.RefillBattery(refillPercent);

        if (consumeOnUse)
        {
            isUsed = true;
            ApplyUsedState();
        }

        Used?.Invoke(this);

        return true;
    }

    public object CaptureState()
    {
        return new BatterySaveData
        {
            id = SaveKey,
            isUsed = isUsed,
            onboardingRevealed = onboardingRevealed
        };
    }

    public void RestoreState(object state)
    {
        if (state is not BatterySaveData data)
            return;

        isUsed = data.isUsed;
        onboardingRevealed = data.onboardingRevealed || isUsed;
        onboardingInteractionEnabled = onboardingRevealed;
        ApplyHighlightVisibility();
        ApplyUsedState();
    }

    private void ApplyUsedState()
    {
        if (consumeOnUse && isUsed && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return gameObject.scene.name + "/" + path;
    }
}
