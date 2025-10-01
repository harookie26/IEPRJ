using UnityEngine;

// Attach to wall GameObjects. Provides a dynamically computed hide anchor (position + normal)
// nearest to the player when requested. Useful when you don't want to place manual child anchors.
[DisallowMultipleComponent]
public class WallHideAnchor : MonoBehaviour
{
    [Tooltip("Optional offset along the normal when the player snaps to this anchor.")]
    public float snapOffset = 0.5f;

    Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
            Debug.LogWarning($"WallHideAnchor on '{name}' has no Collider. It won't provide anchors.");
    }

    // Try to compute a good anchor point & normal for a given player position.
    // Returns false if no collider is present.
    public bool TryGetAnchor(Vector3 playerPosition, out Vector3 anchorPosition, out Vector3 anchorNormal)
    {
        anchorPosition = Vector3.zero;
        anchorNormal = Vector3.up;

        if (_collider == null)
            return false;

        // Closest point on collider surface (handles all collider types)
        Vector3 closest = _collider.ClosestPoint(playerPosition);

        // Direction from player to the surface point
        Vector3 dir = closest - playerPosition;
        float dist = dir.magnitude;

        // If the player is exactly on/inside the collider, fallback to a direction away from collider center
        if (dist <= 0.001f)
        {
            dir = (playerPosition - _collider.bounds.center).normalized;
            if (dir.sqrMagnitude <= 0.0001f)
                dir = transform.forward;
            dist = 0.01f;
        }

        // Raycast from the player toward the surface to get an accurate hit.normal and exact hit point if possible
        RaycastHit hit;
        if (Physics.Raycast(playerPosition, dir.normalized, out hit, dist + 0.25f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Use the raycast hit if it hits this collider (or a child of it)
            if (hit.collider == _collider || hit.collider.transform.IsChildOf(_collider.transform) || _collider.transform.IsChildOf(hit.collider.transform))
            {
                anchorPosition = hit.point + hit.normal.normalized * snapOffset;
                anchorNormal = hit.normal.normalized;
                return true;
            }
        }

        // Fallback: use closest point and approximate normal as direction from surface toward player (so snapping away moves player outward)
        anchorPosition = closest + ((closest - playerPosition).normalized * snapOffset);
        anchorNormal = (closest - playerPosition).normalized;
        if (anchorNormal.sqrMagnitude <= 0.0001f)
            anchorNormal = transform.forward;

        return true;
    }

    // For debugging in editor
    void OnDrawGizmosSelected()
    {
        if (_collider == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_collider.bounds.center, Mathf.Max(0.1f, Mathf.Min(_collider.bounds.extents.magnitude, 0.5f)));
    }
}