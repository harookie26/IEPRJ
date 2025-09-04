using System;
using UnityEngine;

[Serializable]
public class AnimationAction : CutsceneAction
{
    // Changed from fields to properties
    public string TargetAnimatorId { get; set; }
    public string AnimationTrigger { get; set; }

    public override void Execute(Action onComplete)
    {
        // Find the GameObject in the scene with the specified ID
        GameObject targetObject = GameObject.Find(TargetAnimatorId);

        if (targetObject != null)
        {
            Animator targetAnimator = targetObject.GetComponent<Animator>();
            targetAnimator.enabled = true;
            if (targetAnimator != null && !string.IsNullOrEmpty(AnimationTrigger))
            {
                targetAnimator.SetTrigger(AnimationTrigger);
            }
            else
            {
                Debug.LogWarning($"Animator component not found on '{TargetAnimatorId}', or Animation Trigger is not set.");
            }
        }
        else
        {
            Debug.LogWarning($"GameObject with ID '{TargetAnimatorId}' not found in the scene for AnimationAction.");
        }

        onComplete(); // This action completes instantly
    }
}