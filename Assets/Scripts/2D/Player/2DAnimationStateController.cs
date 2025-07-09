using UnityEngine;

public class AnimationStateController2D : MonoBehaviour
{
    Animator animator;
    private PlayerMovement2D playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement2D>();
    }

    void Update()
    {
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
