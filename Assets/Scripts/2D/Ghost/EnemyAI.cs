using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float distance = 5f;
    [SerializeField] private float speed = 7f; // Movement speed

    private bool isPlayerInSight = false;
    private Transform playerTransform;

    void Start()
    {
        isPlayerInSight = false;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        isPlayerInSight = CheckPlayer();

        if (isPlayerInSight)
        {
            ChasePlayer();
        }
    }

    public bool CheckPlayer()
    {
        Hiding_Script hiding_Script = Object.FindFirstObjectByType<Hiding_Script>();

        Vector2 direction = transform.right * Mathf.Sign(transform.localScale.x);

        int playerLayer = LayerMask.GetMask("Player");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, playerLayer);

        if (hit.collider != null && hit.collider.CompareTag("Player") && (hiding_Script.isHiding == false))
        {
            Debug.Log("Player is in front of the enemy!");
            isPlayerInSight = true;
        }
        else
        {
            isPlayerInSight = false;
        }

        Debug.DrawRay(transform.position, direction * distance, Color.red);

        return isPlayerInSight;
    }

    public void ChasePlayer()
    {
        if (playerTransform == null) return;

        // Flip the enemy to face the player
        float direction = playerTransform.position.x - transform.position.x;
        if (direction != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            transform.localScale = scale;
        }

        // Move towards the player's position
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTransform.position,
            speed * Time.deltaTime
        );
    }

}
