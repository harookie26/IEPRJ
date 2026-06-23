using Game.ObjectTypes;
using Level.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Optional completion notification interface channelables can implement.
public interface INotifiesChannelCompletion
{
    event System.Action ChannelCompleted;
    bool IsCompleted { get; }
    string PaintingId { get; }
}

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintingChannelable : MonoBehaviour, IChannelable, INotifiesChannelCompletion
{
    private bool isChanneling;

    [Header("Channeling / Completion")]
    [Tooltip("Seconds required to consider the channeling 'complete'")]
    [SerializeField] private float requiredChannelDuration = 2f;

    [Tooltip("If TRUE, pause the game when the object has been completed consecutively this many times.")]
    [SerializeField] private bool pauseOnConsecutiveCompletions = true;

    //[Tooltip("Number of consecutive completions required to trigger pause.")]
    //[SerializeField] private int consecutiveCompletionsToPause = 2;

    [Header("Cover Object")]
    [Tooltip("Optional child object that acts as a cover and should be disabled upon completion.")]
    [SerializeField] private GameObject coverObject;

    [Header("True Painting Material")]
    [Tooltip("Optional child object that acts as a cover and should be disabled upon completion.")]
    [SerializeField] private Material truePaintingMaterial;

    [Tooltip("How long the crossfade between corrupted and true material should take.")]
    [SerializeField] private float materialFadeDuration = 3.0f;

    [Header("Painting ID")]
    [Tooltip("Unique string ID for this painting. Assign in inspector (e.g. 'paint1').")]
    [SerializeField] private string paintingId = "";

    [Header("Dynamic Cutscene Settings")]
    [Tooltip("Place this transform strictly in front of the visible canvas. If omitted, a child named CutsceneCameraAnchor is used.")]
    [SerializeField] private Transform cutsceneCameraAnchor;
    [Tooltip("Distance from the painting to start the camera.")]
    [SerializeField] private float startDistance = .5f;
    [Tooltip("Distance from the painting to end the camera zoom.")]
    [SerializeField] private float endDistance = 2.0f;
    [Tooltip("Adjust this if the camera is too high or too low (e.g., 1.5 for eye level).")]
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float moveDuration = 4f;
    [SerializeField] private float viewDuration = 5f;

    private AudioSource sfxAudioSource;
    private AudioSource bgmAudioSource;
    private AudioSource restorationMusicAudioSource;
    private AudioList audioList;

    private CheckpointManager checkpointManager => FindFirstObjectByType<CheckpointManager>();

    private float channelTimer = 0f;
    private bool isCompleted = false;

    private static int consecutiveCompletions = 0;

    // Notify listeners (PlayerChanneller) when this target finishes.
    public event System.Action ChannelCompleted;
    public bool IsCompleted => isCompleted;
    public string PaintingId => paintingId;

    void Awake()
    {
        EnsureCutsceneCameraAnchor();

        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");
        GameObject audioObject2 = GameObject.FindWithTag("MusicAudioSource");

        if (audioObject1 != null)
        {
            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        // Get the BGM audio source by tag
        if (audioObject2 != null)
        {
            bgmAudioSource = audioObject2.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'MusicAudioSource' found in scene.");
        }

        // Get the restoration music audio source from AudioList
        if (audioList != null)
        {
            restorationMusicAudioSource = audioList.restorationMusicAudioSource;
            if (restorationMusicAudioSource == null)
            {
                Debug.LogWarning("No restoration music AudioSource assigned in AudioList.");
            }
        }
        else
        {
            Debug.LogWarning("No AudioList instance found in scene.");
        }

        // Auto-assign first child as cover if not explicitly set.
        if (coverObject == null && transform.childCount > 0)
        {
            coverObject = transform.GetChild(0).gameObject;
        }
    }


    public void StartChannel()
    {
        // Do not allow starting channel if already fully completed for this painting.
        if (isCompleted)
        {
            Debug.Log($"[PaintingChannelable] Channeling already completed for painting ID '{paintingId}'. Cannot restart channeling.");
            return;
        }

        if (isChanneling) return;
        isChanneling = true;
        // keep isCompleted unchanged here (should remain false until completed)
        channelTimer = 0f;
        Debug.Log($"[PaintingChannelable] Channel START on '{gameObject.name}' (instance id {GetInstanceID()}).");

        // Pause the BGM
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            //bgmAudioSource.Pause();
            Debug.Log("[PaintingChannelable] BGM paused.");
        }

        // Play the restoration music
        if (restorationMusicAudioSource != null && audioList != null && audioList.paintingRestorationMusic != null)
            restorationMusicAudioSource.PlayOneShot(audioList.paintingRestorationMusic);
    }

    public void StopChannel()
    {
        if (restorationMusicAudioSource != null)
            restorationMusicAudioSource.Stop(); // stop the restoration music if still playing

        // Resume the BGM
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.UnPause();
            Debug.Log("[PaintingChannelable] BGM resumed.");
        }

        if (!isChanneling) return;

        if (!isCompleted)
        {
            consecutiveCompletions = 0;
            Debug.Log($"[PaintingChannelable] Channel STOP (interrupted) on '{gameObject.name}' — consecutive completions reset.");
        }
        else
        {
            Debug.Log($"[PaintingChannelable] Channel STOP after completion on '{gameObject.name}'. Consecutive completions = {consecutiveCompletions}.");
        }

        isChanneling = false;
        // Do NOT reset isCompleted here; once completed the painting remains completed.
        channelTimer = 0f;

        Debug.Log($"[PaintingChannelable] Channel STOP on '{gameObject.name}' (instance id {GetInstanceID()}).");
    }

    private void Update()
    {
        if (isChanneling && !isCompleted)
        {
            channelTimer += Time.deltaTime;

            if (channelTimer >= requiredChannelDuration)
            {
                isCompleted = true;

                // Notify listeners FIRST so the _player can exit ChannelState immediately.
                ChannelCompleted?.Invoke();

                HandleCompletion();
                // Do not call StopChannel() here; PlayerChanneller will call it upon completion.
            }
        }
    }

    private void HandleCompletion()
    {
        if (restorationMusicAudioSource != null)
            restorationMusicAudioSource.Stop(); // stop the restoration music if still playing

        // Resume the BGM
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.UnPause();
            Debug.Log("[PaintingChannelable] BGM resumed.");
        }

        // Disable the cover child object upon completion.
        if (coverObject != null && coverObject.activeSelf)
        {
            coverObject.SetActive(false);
            Debug.Log($"[PaintingChannelable] Cover object '{coverObject.name}' deactivated for '{gameObject.name}'.");
        }

        FindFirstObjectByType<CorruptPaintingRandomizer>().OnPaintingCompleted(gameObject);

        consecutiveCompletions++;
        Debug.Log($"[PaintingChannelable] Channel COMPLETE on '{gameObject.name}'. Consecutive completions = {consecutiveCompletions}.");

        if (CorruptedRoomsManager.Instance != null)
        {
            CorruptedRoomsManager.Instance.RestoreRoomAtPosition(transform.position);
        }
        else
        {
            Debug.LogWarning("[PaintingChannelable] No CorruptedRoomsManager instance found to restore room.");
        }

        if (truePaintingMaterial != null)
        {
            Renderer paintingRenderer = GetComponent<Renderer>();

            if (paintingRenderer != null)
            {
                StartCoroutine(FadeMaterialRoutine(paintingRenderer));
            }
        }

        if (sfxAudioSource != null && audioList != null && audioList.paintingRestorationCompleteSFX != null)
            sfxAudioSource.PlayOneShot(audioList.paintingRestorationCompleteSFX);

        DialoguePlaybackHandle dialoguePlayback = null;

        if (paintingId != "000")
        {
            dialoguePlayback = DialogueTriggerManager.Instance.TriggerPaintingBGDialogue(paintingId);
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.ADD_PAINTING_RESTORED);
        }
        else if (paintingId == "000")
        {
            DialogueTriggerManager.Instance.TriggerFindCorruptedDialogue();
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT4_START);

            StartCoroutine(WaitForInitialCutsceneToFinish());
        }

        CheckpointManager checkpoint = checkpointManager;
        if (checkpoint != null)
            checkpoint.SaveCheckpoint();
        else
            Debug.LogWarning("[PaintingChannelable] No CheckpointManager found. Continuing without saving a checkpoint.");

        PaintingCutsceneDirector director = PaintingCutsceneDirector.Instance;
        if (director == null)
            director = FindFirstObjectByType<PaintingCutsceneDirector>();

        if (director == null)
        {
            Debug.LogWarning("[PaintingChannelable] No PaintingCutsceneDirector found. Skipping cutscene.");
            return;
        }

        director.TryPlay(new PaintingCutsceneRequest(
            transform,
            EnsureCutsceneCameraAnchor(),
            startDistance,
            endDistance,
            heightOffset,
            fadeDuration,
            moveDuration,
            viewDuration,
            dialoguePlayback));

    }

    public Transform EnsureCutsceneCameraAnchor()
    {
        if (cutsceneCameraAnchor != null)
            return cutsceneCameraAnchor;

        cutsceneCameraAnchor = transform.Find("CutsceneCameraAnchor");

        if (cutsceneCameraAnchor == null)
        {
            GameObject anchorObject = new GameObject("CutsceneCameraAnchor");
            cutsceneCameraAnchor = anchorObject.transform;
            cutsceneCameraAnchor.SetParent(transform, false);

            // The painting FBXs in this project are imported with their visible
            // canvas facing local -Y. Project that axis horizontally because the
            // models themselves are mounted with a 90-degree rotation in-scene.
            Vector3 frontDirection = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
            if (frontDirection.sqrMagnitude < 0.0001f)
                frontDirection = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);
            if (frontDirection.sqrMagnitude < 0.0001f)
                frontDirection = Vector3.forward;

            frontDirection.Normalize();
            float anchorDistance = Mathf.Max(startDistance, endDistance, 0.5f);
            cutsceneCameraAnchor.position = transform.position + frontDirection * anchorDistance;
            cutsceneCameraAnchor.rotation = Quaternion.LookRotation(-frontDirection, Vector3.up);
        }

        return cutsceneCameraAnchor;
    }

    private System.Collections.IEnumerator FadeMaterialRoutine(Renderer paintingRenderer)
    {
        Material[] mats = paintingRenderer.materials;

        yield return new WaitForSeconds(2.0f);

        if (mats.Length > 1)
        {
            Material corruptedMat = mats[1];

            Material transitionMat = new Material(corruptedMat);

            transitionMat.EnableKeyword("_EMISSION");

            mats[1] = transitionMat;
            paintingRenderer.materials = mats;

            float halfDuration = materialFadeDuration / 2f;
            float elapsedTime = 0f;

            Color glowColor = Color.white * 3f;

            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / halfDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                if (transitionMat.HasProperty("_EmissionColor"))
                    transitionMat.SetColor("_EmissionColor", Color.Lerp(Color.black, glowColor, eased));

                yield return null;
            }

            if (transitionMat.HasProperty("_EmissionColor"))
                transitionMat.SetColor("_EmissionColor", glowColor);

            if (transitionMat.HasProperty("_BaseMap")) transitionMat.SetTexture("_BaseMap", truePaintingMaterial.GetTexture("_BaseMap"));
            if (transitionMat.HasProperty("_MainTex")) transitionMat.mainTexture = truePaintingMaterial.mainTexture;

            elapsedTime = 0f;

            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / halfDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                if (transitionMat.HasProperty("_EmissionColor"))
                    transitionMat.SetColor("_EmissionColor", Color.Lerp(glowColor, Color.black, eased));

                yield return null;
            }

            mats[1] = truePaintingMaterial;
            paintingRenderer.materials = mats;

            Destroy(transitionMat);

            Debug.Log($"[PaintingChannelable] Successfully finished Emission Flash transition on {gameObject.name}");
        }
    }

    private System.Collections.IEnumerator WaitForInitialCutsceneToFinish()
    {
        // Calculate the total time the first cutscene takes to play.
        // (Fade In + Move + View + Fade Out)
        float totalCutsceneTime = (fadeDuration * 2) + moveDuration + viewDuration + 0.5f;

        // Wait for that exact amount of time
        yield return new WaitForSeconds(totalCutsceneTime);

        if (CorruptedTutorialCutscene.Instance != null)
        {
            CorruptedTutorialCutscene.Instance.StartCorruptedPaintingCutscene();
        }
    }
}
