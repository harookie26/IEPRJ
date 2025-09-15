using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.ObjectTypes; // For ILinkable

public class DisableFarObjects : MonoBehaviour
{
    [SerializeField] private float disableDistance = 50f; // Distance threshold
    private List<GameObject> rooms = new List<GameObject>();

    void Start()
    {
        rooms.AddRange(GameObject.FindGameObjectsWithTag("Room"));
    }

    void Update()
    {
        Vector3 playerPosition = transform.position;
        HashSet<GameObject> roomsToEnable = new HashSet<GameObject>();

        // Step 1: Enable rooms within distance
        foreach (GameObject room in rooms)
        {
            if (room == null) continue;
            float distance = Vector3.Distance(room.transform.position, playerPosition);
            if (distance <= disableDistance)
            {
                roomsToEnable.Add(room);

                // Step 2: Find all Doors components in this room
                var doors = room.GetComponentsInChildren<DoorsComponent>(true);
                foreach (var door in doors)
                {
                    // Step 3: Find linked doors via LinkRegistry
                    var linkedDoors = LinkRegistry
                        .GetLinkedObjects(door.LinkID)
                        .OfType<DoorsComponent>()
                        .Where(d => d.UniqueID != door.UniqueID);

                    foreach (var linkedDoor in linkedDoors)
                    {
                        // Step 4: Enable the room containing the linked door
                        var linkedRoom = linkedDoor.transform.parent.gameObject;
                        roomsToEnable.Add(linkedRoom);
                    }
                }
            }
        }

        // Step 5: Enable/disable rooms
        foreach (GameObject room in rooms)
        {
            if (room == null) continue;
            bool shouldEnable = roomsToEnable.Contains(room);
            if (room.activeSelf != shouldEnable)
            {
                room.SetActive(shouldEnable);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disableDistance);
    }
}
