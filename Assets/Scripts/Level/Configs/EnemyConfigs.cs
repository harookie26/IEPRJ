using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfigs", menuName = "Scriptable Objects/EnemyConfigs")]
public class EnemyConfig : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Movement speed while actively chasing the player (units/sec).")]
    public float moveSpeed = 3f;
    [Tooltip("Patrol / roaming movement speed (units/sec).")]
    public float patrolSpeed = 2f;
    [Tooltip("Extra time (seconds) the enemy waits before giving up pursuit after losing direct contact with the player).")]
    public float targetingBuffer = 1f;
}