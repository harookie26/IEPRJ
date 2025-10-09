using UnityEngine;
using Game.ObjectTypes;

[DisallowMultipleComponent]
[FoldableInspector]
public class PaintingChannelable : MonoBehaviour, IChannelable
{
    private bool isChanneling;

    [Header("Channeling / Completion")]
    [Tooltip("Seconds required to consider the channeling 'complete'")]
    [SerializeField] private float requiredChannelDuration = 2f;

    [Tooltip("If TRUE, pause the game when the object has been completed consecutively this many times.")]
    [SerializeField] private bool pauseOnConsecutiveCompletions = true;

    [Tooltip("Number of consecutive completions required to trigger pause.")]
    [SerializeField] private int consecutiveCompletionsToPause = 2;

    private float channelTimer = 0f;
    private bool isCompleted = false;

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
        isCompleted = false;
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
                HandleCompletion();
            }
        }
    }

    private void HandleCompletion()
    {
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

        if (pauseOnConsecutiveCompletions && consecutiveCompletions >= Mathf.Max(1, consecutiveCompletionsToPause))
        {
            Debug.Log($"[PaintingChannelable] Consecutive completions threshold reached ({consecutiveCompletions}). Pausing game (Time.timeScale = 0).");
            Time.timeScale = 0f;
        }

        // Additional completion effects can be added here.
    }
}