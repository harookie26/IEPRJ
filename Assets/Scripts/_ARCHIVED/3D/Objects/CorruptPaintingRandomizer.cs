using System.Collections.Generic;
using UnityEngine;

public class CorruptPaintingRandomizer : MonoBehaviour
{
    private List<CorruptPaintingSpawn> corruptSpawnPoints;
    [SerializeField] private GameObject[] corruptedPaintings = new GameObject[4];
    [SerializeField] private bool isDebugMode = false;

    private void Awake()
    {
        // Collect all spawn points with the CorruptPaintingSpawn script
        corruptSpawnPoints = new List<CorruptPaintingSpawn>(
            Object.FindObjectsByType<CorruptPaintingSpawn>(FindObjectsSortMode.None)
        );

        if (isDebugMode)
        {
            Debug.Log($"CorruptPaintingRandomizer: {corruptSpawnPoints.Count} spawn points.");
        }


        foreach (var paintingPrefab in corruptedPaintings)
        {
            int i = Random.Range(0, corruptSpawnPoints.Count);
            var spawn = corruptSpawnPoints[i];

            // Only spawn if the spot is empty
            if (!spawn.HasPainting)
            {
                Instantiate(paintingPrefab, spawn.transform);
                spawn.HasPainting = true;

                if (isDebugMode)
                {
                    Debug.Log($"CorruptPaintingRandomizer: Spawned {paintingPrefab.name} at {spawn.name},  {spawn.transform}");
                }
            }
            else if (isDebugMode)
            {
                Debug.Log($"Skipped {spawn.name}, already has a painting.");
            }
        }
    }
}
