using TMPro;
using UnityEngine;

public class PlayerLocationUpdater : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI locationText;

    // Keep these public/serialized if other scripts read them
    public string playerLocationName;
    public int playerLocationID;

    public System.Action<string> OnLocationChanged;

    void Start()
    {
        // Force the initial text display on start
        if (locationText != null) locationText.text = playerLocationName;
    }

    void Update()
    {
        // Kept your text update loop here
        if (locationText != null) locationText.text = playerLocationName;
    }

    public int getplayerLocationID()
    {
        return playerLocationID;
    }

    public string getplayerLocationName()
    {
        return playerLocationName;
    }

    // Unify the logic into one clear entry point
    public void SetNewLocation(string newLocation, int newID)
    {
        if (playerLocationName != newLocation)
        {
            playerLocationName = newLocation;
            playerLocationID = newID;

            // This is what notifies your MinimapManager!
            OnLocationChanged?.Invoke(playerLocationName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            // TryGetComponent is slightly cleaner and faster than GetComponent != null
            if (other.gameObject.TryGetComponent<RoomComponent>(out RoomComponent room))
            {
                // FIX: Instead of setting variables directly, route it through 
                // SetNewLocation so the Minimap Event actually fires.
                SetNewLocation(room.GetCurrentRoomName(), room.Id);
            }
        }
    }
}