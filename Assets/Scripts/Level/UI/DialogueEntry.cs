using UnityEngine;

namespace Level.UI
{
    [CreateAssetMenu(fileName = "DialogueEntry", menuName = "Dialogue/Dialogue Entry")]
    public class DialogueEntry : ScriptableObject
    {
        [Tooltip("Character name who is speaking")]
        public string characterName = "";

        [TextArea(2, 6)] public string text = "";

        [Tooltip("Time in seconds to hold the dialogue at full alpha (excluding fade durations)")]
        public float displayDuration = 3f;

        [Tooltip("Fade in duration in seconds")]
        public float fadeIn = 0.25f;

        [Tooltip("Fade out duration in seconds")]
        public float fadeOut = 0.25f;
    }
}
