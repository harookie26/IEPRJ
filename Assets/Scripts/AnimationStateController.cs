using UnityEngine;

public class AnimationStateController : MonoBehaviour
{
    Animator animator;
    private PlayerMovement playerMovement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W) && !playerMovement.isRotating)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        if (Input.GetKey(KeyCode.A))
        {
            if (playerMovement.isRotating)
            {
                animator.SetBool("isTurningLeft", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            animator.SetBool("isTurningLeft", false);
            animator.SetBool("isWalking", false);
        }

    }
}
