using UnityEngine;

public class AnimationStateController : MonoBehaviour
{
    Animator animator;
    private PlayerMovement playerMovement;

    public bool isTurningWide = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            if (playerMovement.isRotatingWideToLeft)
            {
                animator.SetBool("isTurningWideToLeft", true);
            }
            else if (playerMovement.isRotatingWideToRight)
            {
                animator.SetBool("isTurningWideToRight", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
                animator.SetBool("isWalkAfterTurn", true);
            }

        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (playerMovement.isRotating90OrLess)
            {
                animator.SetBool("isTurningLeft", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            if (playerMovement.isRotating90OrLess)
            {
                animator.SetBool("isTurningRight", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }
        }
        else if (Input.GetKey(KeyCode.S))
        {
            if (playerMovement.isRotatingWideToLeft)
            {
                animator.SetBool("isTurningWideToLeft", true);
            }
            else if (playerMovement.isRotatingWideToRight)
            {
                animator.SetBool("isTurningWideToRight", true);
            }
            else
            {
                animator.SetBool("isWalkAfterTurn", true);
            }

        }
        
        else
        {
            animator.SetBool("isTurningLeft", false);
            animator.SetBool("isTurningRight", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isTurningWideToRight", false);
            animator.SetBool("isTurningWideToLeft", false);
            animator.SetBool("isWalkAfterTurn", false);
        }

        if (playerMovement.isFacingCamera)
        {
            animator.SetBool("isFacingCamera", true);
        }
        else
        {
            animator.SetBool("isFacingCamera", false);
        }
    }
}
