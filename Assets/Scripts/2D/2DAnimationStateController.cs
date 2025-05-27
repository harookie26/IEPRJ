using UnityEngine;

public class AnimationStateController2D : MonoBehaviour
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
        if (Input.GetKey(KeyCode.D))
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            animator.SetBool("isWalking", true);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                animator.SetBool("isRunning", true);
            }
            else
            {
                animator.SetBool("isRunning", false);
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
            animator.SetBool("isWalking", true);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                animator.SetBool("isRunning", true);
            }
            else
            {
                animator.SetBool("isRunning", false);
            }
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }
}
