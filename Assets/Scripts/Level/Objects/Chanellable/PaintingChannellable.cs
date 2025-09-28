using UnityEngine;
using Game.ObjectTypes;

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintingChannelable : MonoBehaviour, IChannelable
{
    // Tracks whether this object is currently considered channeling.
    // PaintbrushChanneller calls StartChannel() every frame while aiming at a target,
    // so we make these idempotent to avoid log spam.
    private bool isChanneling;

    [Header("Channeling / Completion")]
    [Tooltip("Seconds required to consider the channeling 'complete'")]
    [SerializeField] private float requiredChannelDuration = 2f;

    [Tooltip("If TRUE, pause the game when the object has been completed consecutively this many times.")]
    [SerializeField] private bool pauseOnConsecutiveCompletions = true;

    [Tooltip("Number of consecutive completions required to trigger pause.")]
    [SerializeField] private int consecutiveCompletionsToPause = 2;

    // Tracks progress while channeling
    private float channelTimer = 0f;
    private bool isCompleted = false;

    // Count consecutive completions across instances (shared game-wide)
    private static int consecutiveCompletions = 0;

    public void StartChannel()
    {
        if (isChanneling) return;
        isChanneling = true;
        isCompleted = false;
        channelTimer = 0f;
        Debug.Log($"[PaintingChannelable] Channel START on '{gameObject.name}' (instance id {GetInstanceID()}).");
    }

    public void StopChannel()
    {
        if (!isChanneling) return;

        // If we stopped before completing, break the consecutive chain
        if (!isCompleted)
        {
            consecutiveCompletions = 0;
            Debug.Log($"[PaintingChannelable] Channel STOP (interrupted) on '{gameObject.name}' — consecutive completions reset.");
        }
        else
        {
            // Stopped after completion: keep consecutive count (so repeated completes without interruption accumulate)
            Debug.Log($"[PaintingChannelable] Channel STOP after completion on '{gameObject.name}'. Consecutive completions = {consecutiveCompletions}.");
        }

        isChanneling = false;
        isCompleted = false;
        channelTimer = 0f;

        Debug.Log($"[PaintingChannelable] Channel STOP on '{gameObject.name}' (instance id {GetInstanceID()}).");
    }

    private void Update()
    {
        // Only progress the channel timer while channeling and not yet completed
        if (isChanneling && !isCompleted)
        {
            channelTimer += Time.deltaTime;

            if (channelTimer >= requiredChannelDuration)
            {
                isCompleted = true;
                HandleCompletion();
                // Note: we do NOT reset isChanneling here. StopChannel() will be called by PlayerChanneller
                // when the player releases or aim is lost. This preserves the semantics that Start/Stop are input-driven.
            }
        }
    }

    private void HandleCompletion()
    {
        consecutiveCompletions++;
        Debug.Log($"[PaintingChannelable] Channel COMPLETE on '{gameObject.name}'. Consecutive completions = {consecutiveCompletions}.");

        if (pauseOnConsecutiveCompletions && consecutiveCompletions >= Mathf.Max(1, consecutiveCompletionsToPause))
        {
            Debug.Log($"[PaintingChannelable] Consecutive completions threshold reached ({consecutiveCompletions}). Pausing game (Time.timeScale = 0).");
            Time.timeScale = 0f;
        }

        // TODO: Trigger any completion effects here (play sound, spawn, mark objective, etc.)
    }
}