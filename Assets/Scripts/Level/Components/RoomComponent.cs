using UnityEngine;
using System.Collections.Generic;
using Game.Level;

[ExecuteAlways]
public class RoomComponent : MonoBehaviour, IRoom
{
    [SerializeField] private int id;

    private Vector3 center;
    private BoxCollider boxCollider;

    public int Id => id;
    public Vector3 Center => center;
    public Bounds Bounds => boxCollider != null ? boxCollider.bounds : new Bounds();

    // Flag to indicate if the player is inside this room
    public bool IsPlayerInside { get; private set; }

    private void OnValidate()
    {
        UpdateCenterFromCollider();
    }

    private void Reset()
    {
        UpdateCenterFromCollider();
    }

    private void Awake()
    {
        UpdateCenterFromCollider();
    }

    private void UpdateCenterFromCollider()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
            center = boxCollider.bounds.center;
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            // Draw the bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);

            // Draw the center
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(boxCollider.bounds.center, 0.2f);
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