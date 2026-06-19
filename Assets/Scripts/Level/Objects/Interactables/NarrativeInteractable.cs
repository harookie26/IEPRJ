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

    [Header("Dialogue Settings")]
    [Tooltip("Use a timed voice sequence instead of the existing non-voiced dialogue entries.")]
    [SerializeField] private bool useTimedVoiceSequence = false;

    [Tooltip("The voice clip and timed subtitles played when Use Timed Voice Sequence is enabled.")]
    [SerializeField] private VoicedDialogueSequence timedVoiceSequence;

    [Tooltip("The list of dialogue entries that will be displayed when the player interacts with this object.")]
    [SerializeField] private List<DialogueEntry> dialogueList;

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
    private bool isInCooldown = false;
    private SpriteRenderer spriteRenderer;
    private List<Material> runtimeMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();

    private AudioList audioList;
    private AudioSource audioSource;

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
    }

    private void OnDestroy()
    {
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
        if (!hasBeenInteractedWith && !isInCooldown)
        {
            if (useTimedVoiceSequence)
            {
                if (timedVoiceSequence != null)
                {
                    DialogueManager.Instance.DisplaySequence(timedVoiceSequence);
                }
                else
                {
                    Debug.LogWarning(
                        "Timed voice sequence is enabled, but no sequence is assigned to this NarrativeInteractable.",
                        this);
                }
            }
            else if (dialogueList != null && dialogueList.Count > 0)
            {
                var pendingEntries = new List<DialogueEntry>();

                foreach (var entry in dialogueList)
                {
                    if (!DialogueManager.Instance.CheckifEntryAlreadyInQueue(entry.characterName, entry.text))
                    {
                        pendingEntries.Add(entry);
                    }
                }

                if (pendingEntries.Count > 0)
                    DialogueManager.Instance.DisplaySequence(pendingEntries);
            }
            else
            {
                Debug.Log("No dialogue entries assigned to this NarrativeInteractable.");
            }


            if (singleInteractionOnly) hasBeenInteractedWith = true; //set to true to prevent future interactions if this is meant to be a one-time interaction

            isInCooldown = true;
            StartCoroutine(InteractionCooldown()); //start cooldown timer to prevent spamming interactions and overwhelming the dialogue system with duplicate entries if the player clicks multiple times in quick succession

        }

        if (popUpDisplayPanel != null)
        {
            if (popUpDisplayPanel.activeSelf)
            {
                ClosePopUpDisplay();
            }
            else
            {
                OpenPopUpDisplay();
            }
        }
        else
        {
            Debug.Log("No pop-up display panel assigned to this NarrativeInteractable.");
        }

        if (interactableId == "Newspaper")
            if (audioList != null && audioSource != null)
                audioSource.PlayOneShot(audioList.paperPickupSFX);

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
