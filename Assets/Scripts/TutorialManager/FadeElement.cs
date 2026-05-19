using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeElement : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    [Header("Display Settings")]
    [SerializeField] private bool _autoHide = true;
    [SerializeField] private float _visibleDuration = 3.0f;

    private CanvasGroup _canvasGroup;
    private Coroutine _displayCoroutine;

    // Remove Awake completely to avoid reliance on Unity's lifecycle for initialization

    // We use a safe helper method to fetch the CanvasGroup whenever we need it
    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void PlayTutorialSequence()
    {
        // 1. Instantly turn the GameObject on so its coroutines can run
        gameObject.SetActive(true);

        // 2. Safely grab the reference now that it's awake
        EnsureCanvasGroup();

        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }
        _displayCoroutine = StartCoroutine(TutorialSequenceRoutine());
    }

    public void ForceHide()
    {
        EnsureCanvasGroup();

        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }
        _displayCoroutine = StartCoroutine(FadeRoutine(false, _fadeOutDuration));
    }

    private IEnumerator TutorialSequenceRoutine()
    {
        // Fade In
        yield return StartCoroutine(FadeRoutine(true, _fadeInDuration));

        // Wait
        if (_autoHide)
        {
            yield return new WaitForSeconds(_visibleDuration);

            // Fade Out
            yield return StartCoroutine(FadeRoutine(false, _fadeOutDuration));
        }
    }

    private IEnumerator FadeRoutine(bool fadeIn, float duration)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = _canvasGroup.alpha;

        _canvasGroup.interactable = fadeIn;
        _canvasGroup.blocksRaycasts = fadeIn;

        float elapsedTime = 0f;

        if (duration > 0)
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                yield return null;
            }
        }

        _canvasGroup.alpha = targetAlpha;

        if (!fadeIn)
        {
            gameObject.SetActive(false);
        }
    }
}