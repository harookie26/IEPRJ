using UnityEngine;
using static EventNames;

public class AnimationStateController2D : MonoBehaviour
{
    Animator animator;
    private PlayerMovement2D playerMovement;
    private bool isCutsceneActive = false;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, StartCutsceneState);
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, EndCutsceneState);
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement2D>();
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_START, StartCutsceneState);
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_END, EndCutsceneState);
    }

    private void StartCutsceneState()
    {
        isCutsceneActive = true;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }

    private void EndCutsceneState()
    {
        isCutsceneActive = false;
        // Reset the animator state when the cutscene ends
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }

    void Update()
    {
        if (isCutsceneActive)
        {
            return;
        }

        bool isMovingRight = Input.GetKey(KeyCode.D);
        bool isMovingLeft = Input.GetKey(KeyCode.A);
        bool isMoving = isMovingRight || isMovingLeft;

        animator.SetBool("isWalking", isMoving);

        if (isMovingRight)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (isMovingLeft)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        // Use public properties instead of reflection
        bool canRun = false;
        if (playerMovement != null)
        {
            canRun = playerMovement.IsSprinting && !playerMovement.OutOfStamina && !playerMovement.StaminaLocked;
        }

        animator.SetBool("isRunning", canRun);
    }
}
