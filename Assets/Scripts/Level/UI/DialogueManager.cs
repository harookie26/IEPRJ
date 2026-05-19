using System.Collections.Generic;
using UnityEngine;

namespace Level.UI
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private RectTransform parentCanvas; // where labels will be placed
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

            for (int i = 0; i < initialPool; i++)
                pool.Enqueue(CreateNew());
        }

        private DialogueLabel CreateNew()
        {
            var go = Instantiate(labelPrefab.gameObject, parentCanvas != null ? parentCanvas : null);
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

            // If nothing is playing, start processing the queue
            if (!isPlaying)
                ProcessQueue();
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

            // position at center of parent
            if (currentLabel.transform is RectTransform rt && parentCanvas != null)
            {
                rt.SetParent(parentCanvas, false);
                rt.anchoredPosition = Vector2.zero;
            }

            currentLabel.gameObject.SetActive(true);
            currentLabel.Show(request.characterName, request.text, request.fadeIn, request.displayDuration, request.fadeOut);

            // schedule display finish and then process next dialogue
            float totalDuration = request.fadeIn + request.displayDuration + request.fadeOut + 0.05f;
            StartCoroutine(FinishCurrentDialogue(totalDuration));
        }

        System.Collections.IEnumerator FinishCurrentDialogue(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);

            // Return current label to pool
            if (currentLabel != null)
                ReturnLabel(currentLabel);

            currentLabel = null;
            isPlaying = false;

            // Process next dialogue if any are queued
            if (dialogueQueue.Count > 0)
                ProcessQueue();
        }
    }
}
