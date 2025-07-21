using UnityEngine;
using static EventNames;

public class CameraAnimationController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator cinematicBarsAnimator;
    private Animator cameraAnimator;

    private void Awake()
    {
        // Get the Animator component attached to this GameObject.
        cameraAnimator = GetComponent<Animator>();
        
        // Ensure the Animator is not null.
        if (cameraAnimator == null)
        {
            Debug.LogError("Animator component not found on CameraAnimationController.");
        }

        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, OnCutsceneEnd);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_END, OnCutsceneEnd);
    }

    private void OnCutsceneStart()
    {
        cameraAnimator.SetTrigger("cutsceneStart");

        if (cinematicBarsAnimator != null)
        {
            cinematicBarsAnimator.SetTrigger("show");
        }
    }

    private void OnCutsceneEnd()
    {
        cameraAnimator.SetTrigger("cutsceneEnd");

        if (cinematicBarsAnimator != null)
        {
            cinematicBarsAnimator.SetTrigger("hide");
        }
    }
}
