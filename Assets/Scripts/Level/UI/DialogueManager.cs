using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Level.UI
{
    public sealed class DialoguePlaybackHandle
    {
        public bool IsComplete { get; private set; }
        public bool WasInterrupted { get; private set; }

        internal void Complete()
        {
            IsComplete = true;
        }

        internal void Interrupt()
        {
            WasInterrupted = true;
            IsComplete = true;
        }
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private RectTransform parentCanvas; // For overall canvas reference
        [SerializeField, Tooltip("The dedicated empty GameObject inside the canvas where dialogue labels will actually live.")]
        private RectTransform dialogueContainer;
        [SerializeField] private DialogueLabel labelPrefab;
        [SerializeField] private int initialPool = 2;
        [SerializeField, Tooltip("2D AudioSource used for voiced dialogue lines.")]
        private AudioSource voiceAudioSource;

        private struct DialogueRequest
        {
            public string characterName;
            public string text;
            public float fadeIn;
            public float displayDuration;
            public float fadeOut;
            public AudioClip voiceLine;
            public DialoguePlaybackHandle playbackHandle;
        }

        private readonly Queue<DialogueLabel> pool = new Queue<DialogueLabel>();
        private readonly Queue<DialogueRequest> dialogueQueue = new Queue<DialogueRequest>();
        private DialogueLabel currentLabel;
        private DialogueRequest currentRequest;
        private Coroutine finishCoroutine;
        private bool isPlaying = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (voiceAudioSource == null)
                voiceAudioSource = GetComponent<AudioSource>();

            if (labelPrefab == null)
            {
                Debug.LogWarning("DialogueManager: labelPrefab not assigned.");
                return;
            }

            // Fallback safety: If container isn't set, use the parent canvas directly
            if (dialogueContainer == null)
            {
                dialogueContainer = parentCanvas;
                Debug.LogWarning("DialogueManager: Dialogue Container not assigned. Defaulting to Parent Canvas.", this);
            }

            for (int i = 0; i < initialPool; i++)
                pool.Enqueue(CreateNew());
        }

        private DialogueLabel CreateNew()
        {
            // Instantiates directly inside the designated container
            var go = Instantiate(labelPrefab.gameObject, dialogueContainer != null ? dialogueContainer : null);
            go.SetActive(true);
            var label = go.GetComponent<DialogueLabel>();
            // start inactive
            label.gameObject.SetActive(false);
            return label;
        }

        private DialogueLabel GetLabel()
        {
            if (pool.Count > 0)
            {
                var l = pool.Dequeue();
                l.gameObject.SetActive(true);
                return l;
            }
            return CreateNew();
        }

        private void ReturnLabel(DialogueLabel label)
        {
            if (label == null) return;
            label.gameObject.SetActive(false);
            pool.Enqueue(label);
        }

        // Display by ScriptableObject
        public void Display(DialogueEntry entry)
        {
            if (entry == null) return;
            Display(entry.characterName, entry.text, entry.fadeIn, entry.displayDuration, entry.fadeOut);
        }

        public DialoguePlaybackHandle DisplaySequence(IEnumerable<DialogueEntry> entries)
        {
            if (entries == null || labelPrefab == null) return null;

            var requests = new List<DialogueRequest>();
            var playbackHandle = new DialoguePlaybackHandle();

            foreach (DialogueEntry entry in entries)
            {
                if (entry == null) continue;

                AudioClip voiceLine = entry is VoicedDialogueEntry voicedEntry
                    ? voicedEntry.voiceLine
                    : null;

                if (entry is VoicedDialogueEntry && voiceLine == null)
                {
                    Debug.LogWarning(
                        $"DialogueManager: Voiced dialogue entry '{entry.name}' has no voice clip. Using displayDuration.",
                        entry);
                }

                requests.Add(new DialogueRequest
                {
                    characterName = entry.characterName,
                    text = entry.text,
                    fadeIn = entry.fadeIn,
                    displayDuration = entry.displayDuration,
                    fadeOut = entry.fadeOut,
                    voiceLine = voiceLine,
                    playbackHandle = playbackHandle
                });
            }

            if (requests.Count == 0)
            {
                playbackHandle.Complete();
                return playbackHandle;
            }

            ReplaceWithSequence(requests);
            return playbackHandle;
        }

        public void DisplayLatest(
            string characterName,
            string text,
            float fadeIn = 0.25f,
            float hold = 3f,
            float fadeOut = 0.25f)
        {
            ReplaceWithSequence(new[]
            {
                new DialogueRequest
                {
                    characterName = characterName,
                    text = text,
                    fadeIn = fadeIn,
                    displayDuration = hold,
                    fadeOut = fadeOut
                }
            });
        }

        // Display by explicit values with character name
        public void Display(string characterName, string text, float fadeIn = 0.25f, float hold = 3f, float fadeOut = 0.25f)
        {
            if (labelPrefab == null)
            {
                Debug.LogWarning("DialogueManager: labelPrefab not assigned, cannot display dialogue.");
                return;
            }

            var request = new DialogueRequest
            {
                characterName = characterName,
                text = text,
                fadeIn = fadeIn,
                displayDuration = hold,
                fadeOut = fadeOut
            };

            dialogueQueue.Enqueue(request);

            if (!isPlaying)
                ProcessQueue();
        }

        private void ReplaceWithSequence(IEnumerable<DialogueRequest> requests)
        {
            bool shouldInterrupt = isPlaying;

            if (shouldInterrupt)
                StopCurrentDialogue();

            dialogueQueue.Clear();

            foreach (DialogueRequest request in requests)
                dialogueQueue.Enqueue(request);

            ProcessQueue();
        }

        private void StopCurrentDialogue()
        {
            DialoguePlaybackHandle interruptedHandle = currentRequest.playbackHandle;

            if (finishCoroutine != null)
            {
                StopCoroutine(finishCoroutine);
                finishCoroutine = null;
            }

            if (currentLabel != null)
            {
                currentLabel.HideImmediately();
                ReturnLabel(currentLabel);
                currentLabel = null;
            }

            if (voiceAudioSource != null)
                voiceAudioSource.Stop();

            interruptedHandle?.Interrupt();

            foreach (DialogueRequest queuedRequest in dialogueQueue)
                queuedRequest.playbackHandle?.Interrupt();

            currentRequest = default;
            isPlaying = false;
        }

        // Display by explicit values (text only, no character name)
        public void Display(string text, float fadeIn = 0.25f, float hold = 3f, float fadeOut = 0.25f)
        {
            Display("", text, fadeIn, hold, fadeOut);
        }

        private void ProcessQueue()
        {
            if (dialogueQueue.Count == 0 || isPlaying)
                return;

            var request = dialogueQueue.Dequeue();
            currentRequest = request;
            isPlaying = true;

            currentLabel = GetLabel();

            // Set parent to the dedicated container and center it
            if (currentLabel.transform is RectTransform rt && dialogueContainer != null)
            {
                rt.SetParent(dialogueContainer, false);
                rt.anchoredPosition = Vector2.zero;
            }

            currentLabel.gameObject.SetActive(true);

            float holdDuration = request.displayDuration;
            if (request.voiceLine != null)
            {
                holdDuration = Mathf.Max(0f, request.voiceLine.length - request.fadeIn);

                if (voiceAudioSource != null)
                {
                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = request.voiceLine;
                    voiceAudioSource.Play();
                }
                else
                {
                    Debug.LogWarning("DialogueManager: No voice AudioSource assigned. Subtitle timing will still use the clip length.", this);
                }
            }

            currentLabel.Show(request.characterName, request.text, request.fadeIn, holdDuration, request.fadeOut);

            float totalDuration = request.fadeIn + holdDuration + request.fadeOut + 0.05f;
            finishCoroutine = StartCoroutine(FinishCurrentDialogue(totalDuration));
        }

        IEnumerator FinishCurrentDialogue(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);

            if (currentLabel != null)
                ReturnLabel(currentLabel);

            currentLabel = null;
            finishCoroutine = null;
            isPlaying = false;

            if (voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = null;
            }

            DialoguePlaybackHandle finishedHandle = currentRequest.playbackHandle;
            currentRequest = default;

            if (!QueueContainsHandle(finishedHandle))
                finishedHandle?.Complete();

            if (dialogueQueue.Count > 0)
                ProcessQueue();
        }

        private bool QueueContainsHandle(DialoguePlaybackHandle handle)
        {
            if (handle == null)
                return false;

            foreach (DialogueRequest request in dialogueQueue)
            {
                if (request.playbackHandle == handle)
                    return true;
            }

            return false;
        }

        public bool CheckifEntryAlreadyInQueue(string characterName, string text)
        {
            foreach (var request in dialogueQueue)
            {
                if (request.characterName == characterName && request.text == text)
                    return true;
            }
            return false;
        }   
    }
}
