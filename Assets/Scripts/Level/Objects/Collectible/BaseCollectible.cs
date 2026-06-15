using Game.ObjectTypes;
using System.Collections.Generic;
using UnityEngine;

[FoldableInspector]
public class BaseCollectible : MonoBehaviour, ICollectible
{
    [Header("Collectible")]
    [SerializeField, Tooltip("Unique ID for this collectible (set in the Inspector).")]
    private string collectibleId = "Collectible";

    [Header("Levitation Settings")]
    [SerializeField, Tooltip("Should this collectible start levitating automatically.")]
    private bool startLevitating = true;

    [SerializeField, Tooltip("Maximum displacement from the start position.")]
    [Range(0f, 2f)]
    private float amplitude = 0.25f;

    [SerializeField, Tooltip("How fast the collectible bobs (cycles per second).")]
    [Range(0.1f, 5f)]
    private float frequency = 1f;

    [SerializeField, Tooltip("Axis along which the collectible bobs.")]
    private Vector3 bobAxis = Vector3.up;

    [SerializeField, Tooltip("Rotation speed in degrees per second.")]
    private float rotationSpeed = 45f;

    [SerializeField, Tooltip("Optional random time offset so multiple collectibles don't bob in unison.")]
    private float randomizePhase = 0.15f;

    [Header("Highlight / Glow Settings")]
    [SerializeField, Tooltip("Should this collectible pulse its highlight/glow effect?")]
    private bool enableHighlight = true;

    [SerializeField, Tooltip("How fast the highlight pulses (cycles per second).")]
    [Range(0.1f, 5f)]
    private float highlightFrequency = 1.5f;

    [SerializeField, Tooltip("Minimum brightness/alpha (0 is dark/invisible).")]
    [Range(0f, 1f)]
    private float minHighlightIntensity = 0.2f;

    [SerializeField, Tooltip("Maximum brightness/alpha (1 is fully bright/opaque).")]
    [Range(0f, 5f)]
    private float maxHighlightIntensity = 2f;

    [SerializeField, Tooltip("Check this if using a 3D Mesh with an Emissive material. Uncheck for 2D Sprites.")]
    private bool use3DEmission = false;

    [SerializeField, Tooltip("The material property name for shader emission or color.")]
    private string highlightPropertyName = "_EmissionColor";

    // Internal state
    private Vector3 startPosition;
    private float timeAccumulator;
    private bool isLevitating;

    // Renderer references supporting multiple parts/sub-meshes
    private SpriteRenderer spriteRenderer;
    private List<Material> runtimeMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();

    private void Awake()
    {
        startPosition = transform.position;
        timeAccumulator = Random.Range(0f, randomizePhase);
        isLevitating = startLevitating;

        if (enableHighlight)
        {
            SetupHighlightRenderer();
        }
    }

    void Start()
    {
        if (PlayerCollectibleManager.Instance != null)
        {
            PlayerCollectibleManager.Instance.OnCollectiblesLoaded += CheckIfAlreadyCollected;
        }

        CheckIfAlreadyCollected();
    }

    private void OnDestroy()
    {
        if (PlayerCollectibleManager.Instance != null)
        {
            PlayerCollectibleManager.Instance.OnCollectiblesLoaded -= CheckIfAlreadyCollected;
        }

        // Clean up all instantiated materials to prevent memory leaks
        foreach (var mat in runtimeMaterials)
        {
            if (mat != null) Destroy(mat);
        }
    }

    private void CheckIfAlreadyCollected()
    {
        if (PlayerCollectibleManager.Instance != null && PlayerCollectibleManager.Instance.HasCollected(collectibleId))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isLevitating || enableHighlight)
        {
            timeAccumulator += Time.deltaTime;
        }

        if (isLevitating)
            ApplyLevitation();

        if (enableHighlight)
            ApplyHighlightPulse();
    }

    private void ApplyLevitation()
    {
        float bob = Mathf.Sin(timeAccumulator * frequency * Mathf.PI * 2f) * amplitude;
        Vector3 offset = (bobAxis.normalized) * bob;
        transform.position = startPosition + offset;

        if (rotationSpeed != 0f)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void SetupHighlightRenderer()
    {
        if (use3DEmission)
        {
            // Find all MeshRenderers on this object and any of its children pieces
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in meshRenderers)
            {
                // Instantiate local copies of the materials safely
                foreach (var mat in renderer.materials)
                {
                    mat.EnableKeyword("_EMISSION");

                    // String auto-fix safety verification
                    string verifiedPropName = highlightPropertyName;
                    if (!mat.HasProperty(verifiedPropName))
                    {
                        string alt = verifiedPropName.StartsWith("_") ? verifiedPropName.Replace("_", "") : "_" + verifiedPropName;
                        if (mat.HasProperty(alt)) verifiedPropName = alt;
                    }

                    if (mat.HasProperty(verifiedPropName))
                    {
                        Color origColor = mat.GetColor(verifiedPropName);
                        if (origColor == Color.black) origColor = Color.white;

                        runtimeMaterials.Add(mat);
                        originalColors.Add(origColor);
                    }
                }
            }
        }
        else
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                originalColors.Add(spriteRenderer.color);
            }
        }
    }

    private void ApplyHighlightPulse()
    {
        float rawSin = Mathf.Sin(timeAccumulator * highlightFrequency * Mathf.PI * 2f);
        float normalizedValue = (rawSin + 1f) / 2f;
        float currentIntensity = Mathf.Lerp(minHighlightIntensity, maxHighlightIntensity, normalizedValue);

        if (use3DEmission)
        {
            // Loop through and update every single cached material piece simultaneously
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    Color blendedColor = originalColors[i] * currentIntensity;
                    runtimeMaterials[i].SetColor(highlightPropertyName, blendedColor);
                }
            }
        }
        else if (spriteRenderer != null && originalColors.Count > 0)
        {
            Color newColor = originalColors[0];
            newColor.a = currentIntensity;
            spriteRenderer.color = newColor;
        }
    }

    // ICollectible implementation
    public virtual void Collect()
    {
        if (GetID == "Key")
        {
            DialogueTriggerManager.Instance.TriggerKeyFoundDialogue();
        }

        if (GetID == "Paintbucket")
        {
            if (!PlayerCollectibleManager.Instance.HasCollected("Flashlight"))
            {
                DialogueTriggerManager.Instance.TriggerFindFlashlightFirstDialogue();
                return;
            }
            else
            {
                EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT_PAINTING_START);
                DialogueTriggerManager.Instance.TriggerChannelDialogue();
            }
        }

        if (GetID == "Flashlight")
        {
            if (LockedDoorInteractable.Instance.hasOpened == true)
            {
                EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT3_START);
                DialogueTriggerManager.Instance.TriggerPaintbucketDialogue();
            }

            DialogueTriggerManager.Instance.TriggerFlashlightDialogue();
            TutorialManager.Instance.TriggerFlashlightTutorial();
        }

        var manager = FindFirstObjectByType<PlayerCollectibleManager>();
        if (manager != null)
        {
            manager.AddCollected(GetID);
        }
        else
        {
            Debug.LogWarning($"PlayerCollectibleManager not found when collecting {GetID}.");
        }

        OnCollect();

        gameObject.SetActive(false);
    }

    protected virtual void OnCollect() { }

    public virtual void Levitate()
    {
        isLevitating = true;
    }

    public virtual string GetID => collectibleId;
}