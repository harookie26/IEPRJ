using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    [SerializeField] private PlayerLocationUpdater locationUpdater;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private GameObject minimapObject;

    [SerializeField] private List<GameObject> minimapMarkers;

    private bool isMinimapVisible = false;

    private int maskFloor1;
    private int maskFloor2;
    private int maskFloor3;
    private int maskFloor4;

    void Start()
    {
        maskFloor1 = LayerMask.GetMask("Minimap1", "Player Minimap Pointer");
        maskFloor2 = LayerMask.GetMask("Minimap2", "Player Minimap Pointer");
        maskFloor3 = LayerMask.GetMask("Minimap3", "Player Minimap Pointer");
        maskFloor4 = LayerMask.GetMask("Minimap4", "Player Minimap Pointer");

        if (locationUpdater != null)
        {
            // Subscribe to the event
            locationUpdater.OnLocationChanged += UpdateMinimapLayer;
            // Initialize once at the start
            UpdateMinimapLayer(locationUpdater.getplayerLocationName());
        }
    }

    void Update()
    {
        if (SaveCourier.IsLoadingSave) return;

        if (PlayerCollectibleManager.Instance != null && PlayerCollectibleManager.Instance.HasCollected("Map") && !isMinimapVisible)
        {
            isMinimapVisible = true;
            minimapObject.gameObject.SetActive(isMinimapVisible);
        }

    }

    void LateUpdate()
    {
        // Synchronize the rotation after the minimap camera has definitively moved
        foreach (GameObject item in minimapMarkers)
        {
            item.transform.rotation = minimapCamera.transform.rotation;
        }
    }

    private void UpdateMinimapLayer(string newLocation)
    {
        // This function ONLY runs when the player crosses into a new room! 0% CPU waste in Update.
        switch (newLocation)
        {
            case "Reception Area":
            case "Hallway 1":
            case "Gallery 1":
            case "Main Gallery":
            case "Hallway 2":
                minimapCamera.cullingMask = maskFloor1;
                break;

            case "Cafeteria":
            case "Gallery 2":
            case "Gallery 3":
            case "Restroom":
                minimapCamera.cullingMask = maskFloor2;
                break;

            case "Lounge":
                minimapCamera.cullingMask = maskFloor3;
                break;

            case "Maintenance":
            case "Hallway 4":
            case "Office":
            case "Main Gallery Bridge":
            case "Gallery 4":
            case "Hallway 3":
                minimapCamera.cullingMask = maskFloor4;
                break;

            default:
                minimapCamera.cullingMask = maskFloor1;
                break;
        }
    }

    void OnDestroy()
    {
        // Clean up subscription to prevent memory leaks
        if (locationUpdater != null)
        {
            locationUpdater.OnLocationChanged -= UpdateMinimapLayer;
        }
    }
}