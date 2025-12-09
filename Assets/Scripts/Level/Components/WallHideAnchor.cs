using UnityEngine;

// Attach to wall GameObjects. Provides a dynamically computed hide anchor (position + normal)
// nearest to the _player when requested. Useful when you don't want to place manual child anchors.
[DisallowMultipleComponent]
public class WallHideAnchor : MonoBehaviour
{
    [Tooltip("Optional offset along the normal when the player snaps to this anchor.")]
    public float snapOffset = 0.5f;

    [Tooltip("Override outward (normal) direction. Leave zero to use transform.forward.")]
    public Vector3 customNormal;
    [Tooltip("Preferred lateral movement axis (A/D). Leave zero to use transform.right.")]
    public Vector3 customTangent;

    Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
            Debug.LogWarning($"WallHideAnchor on '{name}' has no Collider. It won't provide anchors.");
    }

    // Try to compute a good anchor point & normal for a given _player position.
    // Returns false if no collider is present.
    public bool TryGetAnchor(Vector3 playerPosition, out Vector3 anchorPosition, out Vector3 anchorNormal)
    {
        anchorPosition = Vector3.zero;
        anchorNormal = Vector3.up;

        if (_collider == null)
            return false;

        // Closest point on collider surface (handles all collider types)
        Vector3 closest = _collider.ClosestPoint(playerPosition);

        // Direction from _player to the surface point
        Vector3 dir = closest - playerPosition;
        float dist = dir.magnitude;

        // If the _player is exactly on/inside the collider, fallback to a direction away from collider center
        if (dist <= 0.001f)
        {
            dir = (playerPosition - _collider.bounds.center).normalized;
            if (dir.sqrMagnitude <= 0.0001f)
                dir = transform.forward;
            dist = 0.01f;
        }

        // Raycast from the _player toward the surface to get an accurate hit.normal and exact hit point if possible
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

        // Fallback: use closest point and approximate normal as direction from surface toward _player (so snapping away moves _player outward)
        anchorPosition = closest + ((closest - playerPosition).normalized * snapOffset);
        anchorNormal = (closest - playerPosition).normalized;
        if (anchorNormal.sqrMagnitude <= 0.0001f)
            anchorNormal = transform.forward;

        return true;
    }

    public Vector3 GetNormal() => (customNormal.sqrMagnitude > 0.0001f ? customNormal : transform.forward).normalized;
    public Vector3 GetTangent() => (customTangent.sqrMagnitude > 0.0001f ? customTangent : transform.right).normalized;

    // For debugging in editor
    void OnDrawGizmosSelected()
    {
        if (_collider == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_collider.bounds.center, Mathf.Max(0.1f, Mathf.Min(_collider.bounds.extents.magnitude, 0.5f)));
    }

    // NEW: Returns a stable outward normal & lateral tangent for the face closest to playerPos.
    // Works best with BoxCollider; falls back to raycast/transform axes if needed.
    public bool TryGetDynamicBasis(Vector3 playerPos, out Vector3 normal, out Vector3 tangent)
    {
        normal = Vector3.forward;
        tangent = Vector3.right;

        // 1. If custom overrides are set, use them directly.
        if (customNormal.sqrMagnitude > 0.0001f)
        {
            normal = customNormal.normalized;
            if (customTangent.sqrMagnitude > 0.0001f)
            {
                tangent = Vector3.ProjectOnPlane(customTangent, normal);
                if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(Vector3.up, normal);
                tangent.Normalize();
            }
            else
            {
                tangent = Vector3.Cross(Vector3.up, normal).normalized;
            }
            return true;
        }

        if (_collider == null)
        {
            normal = transform.forward;
            tangent = Vector3.Cross(Vector3.up, normal).normalized;
            return false;
        }

        // 2. Specialized handling for BoxCollider: closest face normal
        if (_collider is BoxCollider box)
        {
            // Transform _player into local space of the box
            Vector3 local = transform.InverseTransformPoint(playerPos);
            Vector3 half = box.size * 0.5f;

            // Compute how "far outside" each axis is to determine dominant face
            float dx = Mathf.Abs(local.x) - half.x;
            float dy = Mathf.Abs(local.y) - half.y;
            float dz = Mathf.Abs(local.z) - half.z;

            // Pick axis with largest penetration or closest face if inside
            float ax = Mathf.Abs(dx);
            float ay = Mathf.Abs(dy);
            float az = Mathf.Abs(dz);

            if (ax >= ay && ax >= az)
                normal = transform.TransformDirection(Mathf.Sign(local.x) * Vector3.right);
            else if (az >= ay)
                normal = transform.TransformDirection(Mathf.Sign(local.z) * Vector3.forward);
            else
                normal = transform.TransformDirection(Mathf.Sign(local.y) * Vector3.up);

            normal.Normalize();

            // Avoid using vertical normals (ceiling/floor) for hiding
            if (Mathf.Abs(normal.y) > 0.95f)
            {
                // Force a horizontal _side normal fallback
                normal = transform.forward;
                if (Mathf.Abs(normal.y) > 0.95f)
                    normal = transform.right;
                normal = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
            }

            tangent = Vector3.Cross(Vector3.up, normal);
            if (tangent.sqrMagnitude < 1e-4f)
                tangent = Vector3.Cross(normal, Vector3.up);
            tangent.Normalize();
            return true;
        }

        // 3. Generic fallback: ray from _player to closest point
        Vector3 closest = _collider.ClosestPoint(playerPos);
        Vector3 dir = (closest - playerPos);
        if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
        dir.Normalize();

        normal = dir; // approximate outward
        // Force horizontal orientation
        Vector3 horizNormal = Vector3.ProjectOnPlane(normal, Vector3.up);
        if (horizNormal.sqrMagnitude > 1e-4f) normal = horizNormal.normalized;

        tangent = Vector3.Cross(Vector3.up, normal);
        if (tangent.sqrMagnitude < 1e-4f)
            tangent = Vector3.Cross(normal, Vector3.up);
        tangent.Normalize();
        return true;
    }
}