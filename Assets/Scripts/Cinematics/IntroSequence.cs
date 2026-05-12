using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class LayerAnimationPlayCheck : CustomYieldInstruction
{
    public override bool keepWaiting => false;
}

public class IntroSequence : MonoBehaviour
{
    [Header("References")]
    public Volume globalVolume;
    public Animator playerAnimator;

    [Header("Settings")]
    public float wakeDuration = 2.0f;
    public int blinkCount = 2;
    public float blinkSpeed = 0.4f;
    [Range(0, 1)] public float finalVignetteIntensity = 0f;

    public string nextSceneName;
    public string lastAnimationStateName = "WakeUp_End";

    private Vignette _vignette;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile.TryGet(out _vignette))
        {
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;

            _vignette.intensity.value = 1.0f;
            _vignette.smoothness.value = 0.5f;

            StartCoroutine(WakeUpSequence());
        }
    }

    IEnumerator WakeUpSequence()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            yield return StartCoroutine(LerpVignette(1.0f, 0.4f, blinkSpeed));
            yield return StartCoroutine(LerpVignette(0.4f, 1.0f, blinkSpeed * 0.5f));
            yield return new WaitForSeconds(0.1f);
        }

        float elapsed = 0f;
        while (elapsed < wakeDuration)
        {
            elapsed += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0, 1, elapsed / wakeDuration);
            _vignette.intensity.value = Mathf.Lerp(1.0f, finalVignetteIntensity, smoothT);
            yield return null;
        }

        _vignette.intensity.value = finalVignetteIntensity;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isFinished", true);

            yield return new LayerAnimationPlayCheck();

            bool isAnimationPlaying = true;
            while (isAnimationPlaying)
            {
                AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(lastAnimationStateName) && stateInfo.normalizedTime >= 1.0f && !playerAnimator.IsInTransition(0))
                {
                    isAnimationPlaying = false;
                }
                yield return null;
            }
        }

        SceneManager.LoadScene(nextSceneName);

        Debug.Log("Wake up sequence complete with blinks.");
    }

    IEnumerator LerpVignette(float start, float end, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            _vignette.intensity.value = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        _vignette.intensity.value = end;
    }
}

