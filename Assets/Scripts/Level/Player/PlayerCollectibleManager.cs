using System.Collections.Generic;
using UnityEngine;

public class PlayerCollectibleManager : MonoBehaviour
{
    // Stores IDs of collected collectibles
    private readonly List<string> _collectedIds = new List<string>();

    // Expose read-only view for other systems (if needed)
    public IReadOnlyList<string> CollectedIds => _collectedIds.AsReadOnly();

    /// <summary>
    /// Register a collectible as collected.
    /// </summary>
    public void AddCollected(string collectibleId)
    {
        if (string.IsNullOrEmpty(collectibleId))
            return;

        if (_collectedIds.Contains(collectibleId))
            return;

        _collectedIds.Add(collectibleId);
        Debug.Log($"Collectible registered: {collectibleId}");
    }

    // Optional helper for checking if already collected
    public bool HasCollected(string collectibleId)
    {
        return _collectedIds.Contains(collectibleId);
    }
}