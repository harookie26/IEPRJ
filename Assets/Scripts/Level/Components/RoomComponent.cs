using UnityEngine;
using Game.Level;
using Unity.VisualScripting;

[FoldableInspector]
[ExecuteAlways]
public class RoomComponent : MonoBehaviour, IRoom
{
    [SerializeField] private int id;
    public string roomName;

    // Explicit bounds collider (so adding another BoxCollider won't break bounds)
    [SerializeField] private BoxCollider boundsCollider;

    [SerializeField] private GameObject particleStripGuide;

    private Vector3 center;
    private PlayerStateMachine playerStateMachine;

    public int Id => id;
    public Vector3 Center => center;

    // Expose bounds and the collider used to compute it
    public Bounds Bounds => boundsCollider != null ? boundsCollider.bounds : new Bounds();
    public BoxCollider BoundsCollider => boundsCollider;

    public bool isCorrupted = false;

    public bool IsPlayerInside { get; private set; }

    private void OnValidate()
    {
        ResolveBoundsCollider();
        UpdateCenterFromCollider();
    }

    private void Reset()
    {
        ResolveBoundsCollider();
        UpdateCenterFromCollider();
    }

    private void Awake()
    {
        ResolveBoundsCollider();
        UpdateCenterFromCollider();

        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
    }

    public string GetCurrentRoomName()
    {
        return roomName;
    }

    private void ResolveBoundsCollider()
    {
        if (boundsCollider != null) return;

        // Auto-pick the largest non-trigger BoxCollider on this GameObject as the bounds.
        var colliders = GetComponents<BoxCollider>();
        if (colliders != null && colliders.Length > 0)
        {
            BoxCollider best = null;
            float bestVol = -1f;

            // Prefer non-trigger; fall back to largest overall if all are triggers.
            foreach (var bc in colliders)
            {
                if (bc == null) continue;
                var size = bc.size;
                var vol = Mathf.Abs(size.x * size.y * size.z);

                if (!bc.isTrigger && vol > bestVol)
                {
                    bestVol = vol;
                    best = bc;
                }
            }

            if (best == null)
            {
                // All triggers: just pick the largest
                foreach (var bc in colliders)
                {
                    if (bc == null) continue;
                    var size = bc.size;
                    var vol = Mathf.Abs(size.x * size.y * size.z);
                    if (vol > bestVol)
                    {
                        bestVol = vol;
                        best = bc;
                    }
                }
            }

            boundsCollider = best;
        }
    }

    private void UpdateCenterFromCollider()
    {
        if (boundsCollider != null)
            center = boundsCollider.bounds.center;
    }

    private void Update()
    {
        if (particleStripGuide == null) return;

        bool isIdle = playerStateMachine != null && playerStateMachine.CurrentState == PlayerStateMachine.PlayerStateKind.Idle;
        bool shouldBeActive = IsPlayerInside && isIdle;

        if (particleStripGuide.activeSelf != shouldBeActive)
            particleStripGuide.SetActive(shouldBeActive);
    }

    private void OnDrawGizmos()
    {
        if (boundsCollider != null)
        {
            // Draw the bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);

            // Draw the center
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(boundsCollider.bounds.center, 0.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInside = false;
        }
    }
}