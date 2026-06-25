using System.Collections.Generic;
using UnityEngine;

namespace Level.UI
{
    [System.Serializable]
    public class TimedDialogueLine
    {
        [Tooltip("Character name who is speaking")]
        public string characterName = "";

        [TextArea(2, 6)] public string text = "";

        [Tooltip("Time in seconds, from the start of the voice clip, when this subtitle begins fading in.")]
        public float startTime = 0f;

        [Tooltip("Time in seconds when this subtitle begins fading out. If 0 or before Start Time, the next line start time or clip length is used.")]
        public float endTime = 0f;

        [Tooltip("Fade in duration in seconds")]
        public float fadeIn = 0.25f;

        [Tooltip("Fade out duration in seconds")]
        public float fadeOut = 0.25f;
    }

    [CreateAssetMenu(fileName = "VoicedDialogueSequence", menuName = "Dialogue/Voiced Dialogue Sequence")]
    public class VoicedDialogueSequence : ScriptableObject
    {
        [Tooltip("Full voice clip for this dialogue sequence.")]
        public AudioClip voiceClip;

        [Tooltip("Subtitle lines timed against the full voice clip.")]
        public List<TimedDialogueLine> lines = new List<TimedDialogueLine>();
    }
}
