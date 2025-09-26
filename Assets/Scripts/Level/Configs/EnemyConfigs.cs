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

    [Header("Combat / Ranges")]
    [Tooltip("Distance (units) at which the enemy becomes aggressive and starts chasing the player.")]
    public float enemyAggroRadius = 8f;

    [Tooltip("Distance (units) at which the enemy will kill the player (or trigger kill behaviour).")]
    public float enemyKillRadius = 1f;

    [Header("Distracted")]
    [Tooltip("How long (seconds) the enemy should remain in Calm after being distracted.")]
    public float distractedCalmDuration = 3f;
}