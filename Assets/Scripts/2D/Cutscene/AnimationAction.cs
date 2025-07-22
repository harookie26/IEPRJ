using System;
using UnityEngine;

[Serializable]
public class AnimationAction : CutsceneAction
{
    public string targetAnimatorId; // Changed from 'Animator targetAnimator'
    public string animationTrigger;

    public override void Execute(Action onComplete)
    {
        // Find the GameObject in the scene with the specified ID
        GameObject targetObject = GameObject.Find(targetAnimatorId);

        if (targetObject != null)
        {
            Animator targetAnimator = targetObject.GetComponent<Animator>();
            targetAnimator.enabled = true;
            if (targetAnimator != null && !string.IsNullOrEmpty(animationTrigger))
            {
                targetAnimator.SetTrigger(animationTrigger);
            }
            else
            {
                Debug.LogWarning($"Animator component not found on '{targetAnimatorId}', or Animation Trigger is not set.");
            }
        }
        else
        {
            Debug.LogWarning($"GameObject with ID '{targetAnimatorId}' not found in the scene for AnimationAction.");
        }

        onComplete(); // This action completes instantly
    }
}