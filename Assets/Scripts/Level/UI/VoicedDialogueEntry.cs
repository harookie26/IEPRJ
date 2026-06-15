using UnityEngine;

namespace Level.UI
{
    [CreateAssetMenu(fileName = "VoicedDialogueEntry", menuName = "Dialogue/Voiced Dialogue Entry")]
    public class VoicedDialogueEntry : DialogueEntry
    {
        [Tooltip("Voice clip played with this subtitle line. The clip length controls the line duration.")]
        public AudioClip voiceLine;
    }
}
