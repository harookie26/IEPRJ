using System.Collections;
using TMPro;
using UnityEngine;

namespace Level.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DialogueLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tmp;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine running;

        void Reset()
        {
            tmp = GetComponentInChildren<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            if (tmp == null) tmp = GetComponentInChildren<TextMeshProUGUI>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show(string message, float fadeIn, float hold, float fadeOut)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Sequence(message, fadeIn, hold, fadeOut));
        }

        public void Show(string characterName, string message, float fadeIn, float hold, float fadeOut)
        {
            string formatted = string.IsNullOrEmpty(characterName) ? message : $"{characterName}: {message}";
            Show(formatted, fadeIn, hold, fadeOut);
        }

        public void HideImmediately()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
            }
        }

        IEnumerator Sequence(string message, float fadeIn, float hold, float fadeOut)
        {
            tmp.text = message;
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(true);

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeIn));
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOut));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
            running = null;
        }
    }
}
