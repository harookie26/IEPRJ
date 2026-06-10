using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Level.UI
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private RectTransform parentCanvas; // For overall canvas reference
        [SerializeField, Tooltip("The dedicated empty GameObject inside the canvas where dialogue labels will actually live.")]
        private RectTransform dialogueContainer;
        [SerializeField] private DialogueLabel labelPrefab;
        [SerializeField] private int initialPool = 2;

        private struct DialogueRequest
        {
            public string characterName;
            public string text;
            public float fadeIn;
            public float displayDuration;
            public float fadeOut;
        }

        private readonly Queue<DialogueLabel> pool = new Queue<DialogueLabel>();
        private readonly Queue<DialogueRequest> dialogueQueue = new Queue<DialogueRequest>();
        private DialogueLabel currentLabel;
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

        public void DisplaySequence(IEnumerable<DialogueEntry> entries)
        {
            if (entries == null || labelPrefab == null) return;

            var requests = new List<DialogueRequest>();

            foreach (DialogueEntry entry in entries)
            {
                if (entry == null) continue;

                requests.Add(new DialogueRequest
                {
                    characterName = entry.characterName,
                    text = entry.text,
                    fadeIn = entry.fadeIn,
                    displayDuration = entry.displayDuration,
                    fadeOut = entry.fadeOut
                });
            }

            if (requests.Count == 0) return;

            ReplaceWithSequence(requests);
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
            isPlaying = true;

            currentLabel = GetLabel();

            // Set parent to the dedicated container and center it
            if (currentLabel.transform is RectTransform rt && dialogueContainer != null)
            {
                rt.SetParent(dialogueContainer, false);
                rt.anchoredPosition = Vector2.zero;
            }

            currentLabel.gameObject.SetActive(true);
            currentLabel.Show(request.characterName, request.text, request.fadeIn, request.displayDuration, request.fadeOut);

            float totalDuration = request.fadeIn + request.displayDuration + request.fadeOut + 0.05f;
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

            if (dialogueQueue.Count > 0)
                ProcessQueue();
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
