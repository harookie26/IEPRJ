using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollectibleManager : MonoBehaviour
{
    // Stores IDs of collected collectibles
    private readonly List<string> _collectedIds = new List<string>();

    // Expose read-only view for other systems (if needed)
    public IReadOnlyList<string> CollectedIds => _collectedIds.AsReadOnly();

    // Event raised when a collectible is added. Passes the collectible ID.
    public event Action<string> CollectibleAdded;

    // Optional static instance accessor for convenience (not mandatory, remains null if no instance exists)
    private static PlayerCollectibleManager _instance;
    public static PlayerCollectibleManager Instance => _instance;

    private void Awake()
    {
        // maintain a simple instance reference (last awake wins)
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

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

        // Raise event
        CollectibleAdded?.Invoke(collectibleId);

        // Show collectible HUD via UIManager (UIManager handles timing and hide)
        var ui = FindObjectOfType<UIManager>();
        if (ui != null)
        {
            ui.ShowCollectibleHUD(collectibleId, 3f);
        }
    }

    // Optional helper for checking if already collected
    public bool HasCollected(string collectibleId)
    {
        return _collectedIds.Contains(collectibleId);
    }
}