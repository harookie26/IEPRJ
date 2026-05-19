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
        [SerializeField] private int initialPool = 4;

        private readonly Queue<DialogueLabel> pool = new Queue<DialogueLabel>();

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

            var label = GetLabel();

            // position at center of parent
            if (label.transform is RectTransform rt && parentCanvas != null)
            {
                rt.SetParent(parentCanvas, false);
                rt.anchoredPosition = Vector2.zero;
            }

            label.gameObject.SetActive(true);
            label.Show(characterName, text, fadeIn, hold, fadeOut);

            // schedule return to pool after total duration
            StartCoroutine(ReturnAfter(label, fadeIn + hold + fadeOut + 0.05f));
        }

        // Display by explicit values (text only, no character name)
        public void Display(string text, float fadeIn = 0.25f, float hold = 3f, float fadeOut = 0.25f)
        {
            Display("", text, fadeIn, hold, fadeOut);
        }

        System.Collections.IEnumerator ReturnAfter(DialogueLabel label, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            ReturnLabel(label);
        }
    }
}
