using TMPro;
using UnityEngine;

public class PlayerLocationUpdater : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI locationText;

    public string playerLocationName;

    public int playerLocationID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        locationText.text = playerLocationName;
    }

    public int getplayerLocationID() 
    {
        Debug.Log(playerLocationID);
        return playerLocationID;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null) {
            if (other.gameObject.GetComponent<RoomComponent>() != null)
            {
                playerLocationName = other.gameObject.GetComponent<RoomComponent>().GetCurrentRoomName();
                playerLocationID = other.gameObject.GetComponent<RoomComponent>().Id;
            }
        }
    }
}
