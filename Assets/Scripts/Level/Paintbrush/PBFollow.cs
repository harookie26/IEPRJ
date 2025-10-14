using UnityEngine;

public class PBFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followSpeed = 5f;

    private Transform player;
    private Vector3 lastPlayerPosition;
    private int side = -1;

    private Rigidbody rb;

    // Collision toggle helpers
    private Collider[] colliders;
    private bool[] colliderEnabledStates;
    private bool originalIsKinematic;
    private bool originalDetectCollisions;
    private RigidbodyConstraints originalConstraints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Cache colliders and their initial enabled states (include children)
        colliders = GetComponentsInChildren<Collider>();
        colliderEnabledStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderEnabledStates[i] = colliders[i].enabled;

        if (rb != null)
        {
            originalIsKinematic = rb.isKinematic;
            originalDetectCollisions = rb.detectCollisions;
            originalConstraints = rb.constraints;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPlayerPosition = player.position;
        }
        else
            Debug.LogError("Player object with tag 'Player' not found in the scene.");

        // Keep physics from rotating the companion by default.
        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    // When this script is enabled (entering Follow mode) disable collisions so the companion can pass through objects.
    private void OnEnable()
    {
        if (rb != null)
        {
            rb.isKinematic = true;               // stop physics forces from moving it
            rb.detectCollisions = false;         // disable collision processing for the rigidbody
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;   // disable colliders so it won't block or push against other colliders
        }
    }

    // Restore original Rigidbody and Collider states when leaving Follow mode (script disabled).
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.isKinematic = originalIsKinematic;
            rb.detectCollisions = originalDetectCollisions;
            rb.constraints = originalConstraints;
            rb.angularVelocity = Vector3.zero; // clear any residual rotation just in case
        }

        if (colliders != null && colliderEnabledStates != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                // guard against destroyed colliders between enable/disable transitions
                if (colliders[i] != null)
                    colliders[i].enabled = colliderEnabledStates[i];
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        float deltaX = player.position.x - lastPlayerPosition.x;
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            int newSide = deltaX > 0 ? -1 : 1;

            if (newSide != side)
            {
                side = newSide;
                offset.x = Mathf.Abs(offset.x) * side;
            }
        }

        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        lastPlayerPosition = player.position;
    }
}