using Game.ObjectTypes;
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

    [Header("Painting ID")]
    [Tooltip("Unique string ID for this painting. Assign in inspector (e.g. 'paint1').")]
    [SerializeField] private string paintingId = "";

    [Header("Dynamic Cutscene Settings")]
    [Tooltip("Distance from the painting to start the camera.")]
    [SerializeField] private float startDistance = .5f;
    [Tooltip("Distance from the painting to end the camera zoom.")]
    [SerializeField] private float endDistance = 2.0f;
    [Tooltip("Adjust this if the camera is too high or too low (e.g., 1.5 for eye level).")]
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float moveDuration = 4f;
    [SerializeField] private float viewDuration = 5f;

    // Hidden from inspector because they will be found automatically via Code
    private Camera cutsceneCamera;
    private UnityEngine.UI.Image fadeImage;

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

        GameObject camObj = GameObject.FindWithTag("CutsceneCamera");
        if (camObj != null)
        {
            cutsceneCamera = camObj.GetComponent<Camera>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'CutsceneCamera' found in scene.");
        }

        GameObject fadeObj = GameObject.FindWithTag("FadeImage");
        if (fadeObj != null)
        {
            fadeImage = fadeObj.GetComponent<UnityEngine.UI.Image>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'FadeImage' found in scene.");
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

        /*if (pauseOnConsecutiveCompletions && consecutiveCompletions >= Mathf.Max(1, consecutiveCompletionsToPause))
        {
            Debug.Log($"[PaintingChannelable] Consecutive completions threshold reached ({consecutiveCompletions}). Pausing game (Time.timeScale = 0).");
            Time.timeScale = 0f;
        }*/

        // Additional completion effects can be added here.
        if (sfxAudioSource != null && audioList != null && audioList.paintingRestorationCompleteSFX != null)
            sfxAudioSource.PlayOneShot(audioList.paintingRestorationCompleteSFX);

        if(paintingId != "000") // Only post the event if a valid painting ID is assigned.
        {
            DialogueTriggerManager.Instance.TriggerPaintingBGDialogue(paintingId);
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.ADD_PAINTING_RESTORED);
        }
        else if(paintingId == "000")
        {
            DialogueTriggerManager.Instance.TriggerFindCorruptedDialogue();
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT4_START);
        }

        checkpointManager.SaveCheckpoint();

        StartCoroutine(PlayCutsceneCoroutine());
    }

    private System.Collections.IEnumerator PlayCutsceneCoroutine()
    {
        if (cutsceneCamera == null || fadeImage == null)
        {
            Debug.LogWarning("[PaintingChannelable] Cutscene dependencies not found. Skipping cutscene.");
            yield break;
        }

        // 1. Calculate Positions Dynamically
        Vector3 outwardDir = GetTrueOutwardDirection();
        Vector3 paintingCenter = transform.position + new Vector3(0, heightOffset, 0);

        Vector3 startPos = paintingCenter + (outwardDir * startDistance);
        Vector3 endPos = paintingCenter + (outwardDir * endDistance);

        // 2. Setup Camera
        Camera mainCam = Camera.main;
        if (mainCam != null) mainCam.enabled = false;

        cutsceneCamera.transform.position = startPos;
        cutsceneCamera.transform.LookAt(paintingCenter);
        cutsceneCamera.enabled = true;

        // 3. Fade IN from Black
        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeDuration));

        // 4. Move the camera backwards dynamically
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            cutsceneCamera.transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveDuration);
            cutsceneCamera.transform.LookAt(paintingCenter); // Keep focused on the center
            elapsed += Time.deltaTime;
            yield return null;
        }
        cutsceneCamera.transform.position = endPos;

        // 5. Wait
        yield return new WaitForSeconds(viewDuration);

        // 6. Fade OUT to Black
        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));

        // 7. Revert Cameras
        cutsceneCamera.enabled = false;
        if (mainCam != null) mainCam.enabled = true;

        // 8. Fade IN back to gameplay
        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeDuration));
    }

    private Vector3 GetTrueOutwardDirection()
    {
        PaintbrushChanneller player = FindFirstObjectByType<PaintbrushChanneller>();
        Vector3 directionToPlayer;

        if (player != null)
        {
            // Get the direction to the player, but IGNORE the Y axis (height).
            // This ensures we only care about their horizontal placement in the room.
            directionToPlayer = player.transform.position - transform.position;
            directionToPlayer.y = 0;
            directionToPlayer.Normalize();
        }
        else
        {
            // Fallback
            directionToPlayer = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        }

        // Check all 6 local axes of the 3D model
        Vector3[] axes = new Vector3[]
        {
            transform.forward, -transform.forward,
            transform.up, -transform.up,
            transform.right, -transform.right
        };

        Vector3 bestAxis = transform.forward;
        float maxDot = -Mathf.Infinity;

        foreach (Vector3 axis in axes)
        {
            // Flatten the axis (remove its vertical Y component)
            Vector3 flatAxis = new Vector3(axis.x, 0, axis.z);

            // If the axis was pointing completely straight up or down, its flat length is 0. Skip it.
            if (flatAxis.sqrMagnitude < 0.01f) continue;

            flatAxis.Normalize();

            // Find which flat axis aligns most perfectly with the player
            float dot = Vector3.Dot(flatAxis, directionToPlayer);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestAxis = flatAxis;
            }
        }

        // Return the best axis. Because it's flattened, the camera will never move vertically.
        return bestAxis.normalized;
    }

    private System.Collections.IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            fadeImage.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }
}
