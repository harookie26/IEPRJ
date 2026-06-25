using Game.ObjectTypes;
using Level.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[FoldableInspector]
public class NarrativeInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactableId;

    [Header("Interaction Audio Settings")]
    [Tooltip("SFX played when this object is interacted with. Pickable objects play these only when picked up.")]
    [SerializeField] private List<AudioClip> interactionSFX = new();

    [Tooltip("Prevent the interaction SFX from playing again after they have played once.")]
    [SerializeField] private bool playSFXOnce = false;

    [Header("Dialogue Settings")]
    [Tooltip("The list of dialogue entries that will be displayed when the player interacts with this object.")]
    [SerializeField] private List<DialogueEntry> dialogueList;

    [Header("Pickup Settings")]
    [Tooltip("Allow this object to be picked up for inspection. Disable for a plain interaction without a popup.")]
    [SerializeField] private bool canBePickedUp = false;

    [Tooltip("The panel that will display UI Object when the player interacts with this object. e.g. newspaper, photos")]
    [SerializeField] private GameObject popUpDisplayPanel;

    [SerializeField] private bool singleInteractionOnly = false;

    [SerializeField] private float dialogueInteractionCooldown = 0.5f; // Minimum time between interactions to prevent spamming

    [Header("Highlight / Glow Settings")]
    [SerializeField, Tooltip("Should this interactable pulse its highlight/glow effect?")]
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

    // Internal tracking state
    private float timeAccumulator;
    private bool hasBeenInteractedWith = false;
    private bool hasPlayedInteractionSFX = false;
    private bool isInCooldown = false;
    private SpriteRenderer spriteRenderer;
    private List<Material> runtimeMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();

    private AudioList audioList;
    private AudioSource audioSource;
    private AudioSource pickableInteractionSFXSource;

    private void Awake()
    {
        // Add a tiny random offset so nearby interactables don't pulse perfectly in unison
        timeAccumulator = UnityEngine.Random.Range(0f, 0.5f);

        if (enableHighlight)
        {
            SetupHighlightRenderer();
        }

        audioList = FindFirstObjectByType<AudioList>();
        audioSource = GetComponent<AudioSource>();

        if (canBePickedUp)
            SetupPickableInteractionSFXSource();
    }

    private void OnDestroy()
    {
        StopPickableInteractionSFX();
        dialogueList = null;
        ClosePopUpDisplay();

        // Clean up all instantiated materials to prevent memory leaks in the editor/build
        foreach (var mat in runtimeMaterials)
        {
            if (mat != null) Destroy(mat);
        }
    }

    private void Update()
    {
        if (enableHighlight)
        {
            timeAccumulator += Time.deltaTime;
            ApplyHighlightPulse();
        }
    }

    private void SetupHighlightRenderer()
    {
        if (use3DEmission)
        {
            // Find all MeshRenderers on this object and any child sub-meshes
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in meshRenderers)
            {
                foreach (var mat in renderer.materials)
                {
                    mat.EnableKeyword("_EMISSION");

                    // String auto-fix safety verification matching BaseCollectible
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

    public void Interact()
    {
        bool pickedUpThisInteraction = false;

        if (canBePickedUp)
        {
            if (popUpDisplayPanel == null)
            {
                Debug.LogWarning(
                    "This NarrativeInteractable can be picked up, but no pop-up display panel is assigned.",
                    this);
            }
            else if (popUpDisplayPanel.activeSelf)
            {
                ClosePopUpDisplay();
            }
            else
            {
                OpenPopUpDisplay();
                pickedUpThisInteraction = true;
            }
        }

        if (!canBePickedUp || pickedUpThisInteraction)
            TryPlayInteractionContent();

        if (pickedUpThisInteraction && interactableId == "Newspaper")
            if (audioList != null && audioSource != null)
                audioSource.PlayOneShot(audioList.paperPickupSFX);
    }

    private void TryPlayInteractionContent()
    {
        if (hasBeenInteractedWith || isInCooldown)
            return;

        if (!playSFXOnce || !hasPlayedInteractionSFX)
            PlayInteractionSFX();

        if (dialogueList != null && dialogueList.Count > 0)
        {
            var pendingEntries = new List<DialogueEntry>();

            foreach (var entry in dialogueList)
            {
                if (!DialogueManager.Instance.CheckifEntryAlreadyInQueue(entry.characterName, entry.text))
                    pendingEntries.Add(entry);
            }

            if (pendingEntries.Count > 0)
                DialogueManager.Instance.DisplaySequence(pendingEntries);
        }
        else
        {
            Debug.Log("No dialogue entries assigned to this NarrativeInteractable.");
        }

        if (singleInteractionOnly)
            hasBeenInteractedWith = true;

        isInCooldown = true;
        StartCoroutine(InteractionCooldown());
    }

    private void PlayInteractionSFX()
    {
        if (interactionSFX == null || interactionSFX.Count == 0)
            return;

        bool playedAnyClip = false;
        foreach (AudioClip clip in interactionSFX)
        {
            if (clip == null)
                continue;

            if (canBePickedUp && pickableInteractionSFXSource != null)
            {
                pickableInteractionSFXSource.PlayOneShot(clip);
                playedAnyClip = true;
            }
            else if (AudioList.Current != null)
            {
                AudioList.Current.PlaySFX(clip);
                playedAnyClip = true;
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
                playedAnyClip = true;
            }
        }

        hasPlayedInteractionSFX |= playedAnyClip;
    }

    private void SetupPickableInteractionSFXSource()
    {
        pickableInteractionSFXSource = gameObject.AddComponent<AudioSource>();
        pickableInteractionSFXSource.playOnAwake = false;
        pickableInteractionSFXSource.loop = false;
        pickableInteractionSFXSource.spatialBlend = 0f;

        GameObject globalSFXObject = GameObject.FindWithTag("SFXAudioSource");
        AudioSource globalSFXSource = globalSFXObject != null
            ? globalSFXObject.GetComponent<AudioSource>()
            : null;

        if (globalSFXSource != null)
        {
            pickableInteractionSFXSource.outputAudioMixerGroup = globalSFXSource.outputAudioMixerGroup;
            pickableInteractionSFXSource.volume = globalSFXSource.volume;
        }
    }

    private void StopPickableInteractionSFX()
    {
        if (pickableInteractionSFXSource != null)
            pickableInteractionSFXSource.Stop();
    }

    private IEnumerator InteractionCooldown()
    {
        yield return new WaitForSeconds(dialogueInteractionCooldown);
        isInCooldown = false;
    }

    public void ClosePopUpDisplay()
    {
        if (popUpDisplayPanel != null && popUpDisplayPanel.activeSelf)
        {
            popUpDisplayPanel.SetActive(false);
            StopPickableInteractionSFX();
        }
    }

    public void OpenPopUpDisplay()
    {
        if (popUpDisplayPanel != null && !popUpDisplayPanel.activeSelf)
        {
            popUpDisplayPanel.SetActive(true);
        }
    }

    // Helper property so the PlayerInteractor can check if the pop-up is active
    public bool IsPopUpOpen => popUpDisplayPanel != null && popUpDisplayPanel.activeSelf;
}
