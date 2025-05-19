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
            if (playerMovement.isRotatingWide)
            {
                animator.SetBool("isTurningWide", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
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
            if (playerMovement.isRotatingWide)
            {
                animator.SetBool("isTurningWide", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }

        }
        else
        {
            animator.SetBool("isTurningLeft", false);
            animator.SetBool("isTurningRight", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isTurningWide", false);
        }
    }
}
