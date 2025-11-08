using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class ScreenFader : MonoBehaviour
{
    [Header("Fade Transition Settings")]
    [Tooltip("Image component used for fade effect")]
    [SerializeField] private Image fadeImage;

    [Tooltip("Optional CanvasGroup to fade instead of the Image (set to fade entire canvas)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Duration of the fade in/out effect in seconds")]
    [SerializeField] private float fadeDuration = 1f;

    [Tooltip("Time to hold the black screen between fades in seconds")]
    [SerializeField] private float blackScreenHoldTime = 1f;

    [Tooltip("Tween easing")]
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Tooltip("If true, use unscaled time (useful during pause)")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Block input while fading (uses Image.raycastTarget or CanvasGroup.blocksRaycasts)")]
    [SerializeField] private bool blockInputDuringFade = true;

    private Tween currentTween;

    private void Start()
    {
        StartCoroutine(FadeInSequence());
    }

    public IEnumerator FadeInOutSequence()
    {
        ActivateFader(true);
        yield return FadeOut();
        yield return Wait(blackScreenHoldTime);
        yield return FadeIn();
        ActivateFader(false);
    }

    public IEnumerator FadeInSequence()
    {
        yield return Wait(blackScreenHoldTime);
        yield return FadeIn();
        ActivateFader(false);
    }

    public IEnumerator FadeOutSequence()
    {
        ActivateFader(true);
        yield return FadeOut();
        yield return Wait(blackScreenHoldTime);
    }

    private void ActivateFader(bool active)
    {
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(active);
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.gameObject.SetActive(active);
    }

    private IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);
    }

    public IEnumerator FadeOut()
    {
        KillCurrentTween();

        PrepareForFade(true);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            currentTween = fadeCanvasGroup.DOFade(1f, fadeDuration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject);
        }
        else if (fadeImage != null)
        {
            var col = fadeImage.color;
            col.a = 0f;
            fadeImage.color = col;
            currentTween = fadeImage.DOFade(1f, fadeDuration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject);
        }

        if (currentTween != null)
            yield return currentTween.WaitForCompletion();
    }

    public IEnumerator FadeIn()
    {
        KillCurrentTween();

        PrepareForFade(true);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            currentTween = fadeCanvasGroup.DOFade(0f, fadeDuration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject);
        }
        else if (fadeImage != null)
        {
            var col = fadeImage.color;
            col.a = 1f;
            fadeImage.color = col;
            currentTween = fadeImage.DOFade(0f, fadeDuration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject);
        }

        if (currentTween != null)
            yield return currentTween.WaitForCompletion();

        PrepareForFade(false);
    }

    private void PrepareForFade(bool starting)
    {
        if (blockInputDuringFade)
        {
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.blocksRaycasts = starting;
                fadeCanvasGroup.interactable = !starting ? true : false;
            }
            else if (fadeImage != null)
            {
                fadeImage.raycastTarget = starting;
            }
        }
    }

    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
    }

    private void OnDisable()
    {
        KillCurrentTween();
    }
}