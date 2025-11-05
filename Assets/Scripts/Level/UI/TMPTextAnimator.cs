using System.Collections;
using TMPro;
using UnityEngine;

namespace Level.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPTextAnimator : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        void Reset() => tmp = GetComponent<TextMeshProUGUI>();

        void Awake()
        {
            if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
        }

        public void PlayTypewriter(string message, float charDelay = 0.05f)
        {
            StopAllCoroutines();
            StartCoroutine(TypewriterCoroutine(message, charDelay));
        }

        public void PlayFadeInWhole(string message, float duration = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeWholeCoroutine(message, duration));
        }

        public void PlayPerCharacterFade(string message, float charDelay = 0.03f, float fadeDuration = 0.2f)
        {
            StopAllCoroutines();
            StartCoroutine(PerCharacterFadeCoroutine(message, charDelay, fadeDuration));
        }

        IEnumerator TypewriterCoroutine(string message, float charDelay)
        {
            tmp.text = message;
            tmp.ForceMeshUpdate();
            tmp.maxVisibleCharacters = 0;
            int total = tmp.textInfo.characterCount;
            for (int i = 1; i <= total; i++)
            {
                tmp.maxVisibleCharacters = i;
                yield return new WaitForSeconds(charDelay);
            }
        }

        IEnumerator FadeWholeCoroutine(string message, float duration)
        {
            tmp.text = message;
            tmp.ForceMeshUpdate();

            var cg = tmp.GetComponent<CanvasGroup>();
            if (cg == null) cg = tmp.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        IEnumerator PerCharacterFadeCoroutine(string message, float charDelay, float fadeDuration)
        {
            tmp.text = message;
            tmp.ForceMeshUpdate();
            var textInfo = tmp.textInfo;
            int charCount = textInfo.characterCount;
            if (charCount == 0) yield break;

            for (int i = 0; i < charCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;
                int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
                int vertIdx = textInfo.characterInfo[i].vertexIndex;
                var colors = textInfo.meshInfo[matIdx].colors32;
                Color32 c = colors[vertIdx];
                c.a = 0;
                colors[vertIdx + 0] = c;
                colors[vertIdx + 1] = c;
                colors[vertIdx + 2] = c;
                colors[vertIdx + 3] = c;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.colors32 = meshInfo.colors32;
                tmp.UpdateGeometry(meshInfo.mesh, i);
            }

            for (int i = 0; i < charCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    yield return null;
                    continue;
                }

                int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
                int vertIdx = textInfo.characterInfo[i].vertexIndex;
                var colors = textInfo.meshInfo[matIdx].colors32;

                byte targetAlpha = (byte)Mathf.Clamp01(tmp.color.a) == 1 ? (byte)255 : (byte)(tmp.color.a * 255);

                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    byte a = (byte)Mathf.Clamp(Mathf.Lerp(0, targetAlpha, t / fadeDuration), 0, 255);
                    Color32 col = colors[vertIdx];
                    col.a = a;
                    colors[vertIdx + 0] = col;
                    colors[vertIdx + 1] = col;
                    colors[vertIdx + 2] = col;
                    colors[vertIdx + 3] = col;

                    tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    yield return null;
                }

                // ensure fully visible
                Color32 finalCol = colors[vertIdx];
                finalCol.a = targetAlpha;
                colors[vertIdx + 0] = finalCol;
                colors[vertIdx + 1] = finalCol;
                colors[vertIdx + 2] = finalCol;
                colors[vertIdx + 3] = finalCol;
                tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                yield return new WaitForSeconds(charDelay);
            }
        }
    }
}