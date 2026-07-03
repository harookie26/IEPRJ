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
    [SerializeField, Min(0.1f)] private float requiredReplacementDuration = 5.5f;
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
    private bool highlightUnlocked;
    private FlashlightOnboardingDirector onboardingDirector;
    private float highlightTime;
    private readonly List<Material> runtimeMaterials = new();
    private readonly List<Color> originalEmissionColors = new();
    private readonly List<string> emissionPropertyNames = new();

    public string SaveKey => string.IsNullOrWhiteSpace(batteryId) ? GetHierarchyPath() : batteryId;
    public float RequiredReplacementDuration => requiredReplacementDuration;
    public bool IsUsed => isUsed;
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

    private void Start()
    {
        onboardingDirector = FindBatteryOnboardingDirector();

        // Duplicating the onboarding battery also duplicates its serialized wait
        // flag. Only the battery owned by a director should remain gated; every
        // other battery must behave like a normal recharge pickup.
        if (waitForFlashlightDepletion && !HasOnboardingDirector())
        {
            waitForFlashlightDepletion = false;
            onboardingRevealed = true;
            onboardingInteractionEnabled = true;
        }

        RefreshHighlightAvailability();
        ApplyHighlightVisibility();
    }

    private void Update()
    {
        RefreshHighlightAvailability();

        if (!enableHighlight
            || (waitForFlashlightDepletion && !onboardingRevealed)
            || (!waitForFlashlightDepletion && !highlightUnlocked))
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
        // Imported models may use a different Renderer subtype and can contain
        // disabled child renderers. Include both so swapping the visual model does
        // not silently disconnect the highlight.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                string propertyName = GetEmissionPropertyName(material);
                if (string.IsNullOrEmpty(propertyName))
                    continue;

                material.EnableKeyword("_EMISSION");

                Color originalColor = material.GetColor(propertyName);
                // Imported FBX materials commonly store black emission with alpha
                // zero. Color.black has alpha one, so an exact Color comparison
                // leaves that material permanently black when pulsed.
                if (Mathf.Max(originalColor.r, originalColor.g, originalColor.b) <= 0.001f)
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
            ? highlightPropertyName.Substring(1)
            : "_" + highlightPropertyName;

        if (material.HasProperty(alternateName))
            return alternateName;

        // HDRP and some imported shaders use "Emissive" rather than "Emission".
        const string emissiveColor = "_EmissiveColor";
        return material.HasProperty(emissiveColor) ? emissiveColor : string.Empty;
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

        bool visible = waitForFlashlightDepletion
            ? onboardingRevealed
            : highlightUnlocked;
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material material = runtimeMaterials[i];
            if (material != null)
                material.SetColor(emissionPropertyNames[i], visible ? originalEmissionColors[i] * minHighlightIntensity : Color.black);
        }
    }

    private bool HasOnboardingDirector()
    {
        FlashlightOnboardingDirector[] directors = FindObjectsByType<FlashlightOnboardingDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (FlashlightOnboardingDirector director in directors)
        {
            if (director != null && director.Manages(this))
                return true;
        }

        return false;
    }

    private FlashlightOnboardingDirector FindBatteryOnboardingDirector()
    {
        FlashlightOnboardingDirector[] directors = FindObjectsByType<FlashlightOnboardingDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (FlashlightOnboardingDirector director in directors)
        {
            if (director != null && director.Manages(this))
                return director;
        }

        // Ordinary batteries are not directly managed, but still use the scene's
        // battery onboarding director as their highlight unlock source.
        return directors.Length > 0 ? directors[0] : null;
    }

    private void RefreshHighlightAvailability()
    {
        if (highlightUnlocked || waitForFlashlightDepletion)
            return;

        onboardingDirector ??= FindBatteryOnboardingDirector();
        if (onboardingDirector != null && onboardingDirector.IsBatteryOnboardingComplete)
        {
            highlightUnlocked = true;
            ApplyHighlightVisibility();
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
