using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video; // Added this namespace to access the VideoPlayer

public class EndingSequence : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetSceneName;

    [Tooltip("How long to wait for the scene's animation to finish before fading to black.")]
    public float initialAnimationDelay = 50f;

    [Tooltip("How many seconds to wait on a black screen at the very end before changing scenes.")]
    public float delayBeforeSceneChange = 3f;

    [Header("Timing Settings")]
    public float fadeDuration = 2f;

    [Header("UI & Video References")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private VideoPlayer endingVideoPlayer; 

    void Start()
    {
        fadeImage.color = new Color(0, 0, 0, 0);

        StartCoroutine(MasterEndingSequence());
    }

    private IEnumerator MasterEndingSequence()
    {
        yield return new WaitForSeconds(initialAnimationDelay);

        yield return StartCoroutine(FadeImage(0f, 1f, fadeDuration));

        if (endingVideoPlayer != null)
        {
            endingVideoPlayer.Play();

            yield return null;

            while (endingVideoPlayer.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("No VideoPlayer assigned to EndingSequence!");
        }

        yield return new WaitForSeconds((float) endingVideoPlayer.clip.length);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("Target Scene Name is empty on " + gameObject.name);
        }
    }

    private IEnumerator FadeImage(float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }
}