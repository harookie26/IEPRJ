using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class ScreenFader : MonoBehaviour
{
    [Header("Fade Transition Settings")]
    [Tooltip("Image component used for fade effect")]
    [SerializeField] private Image fadeImage;

    [Tooltip("Duration of the fade in/out effect in seconds")]
    [SerializeField] private float fadeDuration = 1f;

    [Tooltip("Tween easing")]
    [SerializeField] private Ease ease = Ease.InOutSine;

    [Tooltip("If true, use unscaled time (useful during pause)")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Block input while fading (uses Image.raycastTarget or CanvasGroup.blocksRaycasts)")]
    [SerializeField] private bool blockInputDuringFade = true;

    private Tween currentTween;

    private void Start()
    {
        StartCoroutine(FadeInSequence(1.0f));
    }

    public IEnumerator FadeInOutSequence(float blackScreenHoldTime)
    {
        ActivateFader(true);
        yield return FadeOut();
        yield return Wait(blackScreenHoldTime);
        yield return FadeIn();
        ActivateFader(false);
    }

    public IEnumerator FadeInSequence(float blackScreenHoldTime)
    {
        ActivateFader(true);

        yield return Wait(blackScreenHoldTime);
        yield return FadeIn();
        ActivateFader(false);
    }

    public IEnumerator FadeOutSequence(float blackScreenHoldTime)
    {
        ActivateFader(true);
        yield return FadeOut();
        yield return Wait(blackScreenHoldTime);
    }

    private void ActivateFader(bool active)
    {
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(active);
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

        if (fadeImage != null)
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
        
        if (fadeImage != null)
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
            if (fadeImage != null)
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