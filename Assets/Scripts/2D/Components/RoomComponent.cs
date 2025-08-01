using UnityEngine;
using System.Collections.Generic;
using Game.Level;

public class RoomComponent : MonoBehaviour, IRoom
{
    [SerializeField] private int id;
    [SerializeField] private Vector2 center;

    private Collider2D roomCollider;

    private List<IDoor> connectedDoors = new List<IDoor>();
    public int Id => id;
    public Vector2 Center => center;
    public IEnumerable<IDoor> ConnectedDoors => connectedDoors;
    public Bounds Bounds => roomCollider.bounds;
    private void Awake()
    {
        RoomRegistry.RegisterRoom(this);
        roomCollider = GetComponent<Collider2D>();
    }

    public void AddDoor(IDoor door)
    {
        if (!connectedDoors.Contains(door))
            connectedDoors.Add(door);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, transform.position.z), 0.3f);
        UnityEditor.Handles.Label(new Vector3(center.x, center.y, transform.position.z + 0.1f), $"Room {id}");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.cyan;
            Bounds bounds = col.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
#endif
}