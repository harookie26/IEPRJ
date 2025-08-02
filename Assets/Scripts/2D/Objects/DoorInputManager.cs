using UnityEngine;
using System.Linq;
using static EventNames;

public class DoorInputManager : MonoBehaviour
{
    private GameObject _player;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (InputManager.Instance.WasInteractPressed())
        {
            Debug.Log("[DoorInputManager] W key pressed");

            if (Doors.CurrentDoor?.IsReadyToUse() == true)
            {
                Debug.Log($"[DoorInputManager] Teleporting via {Doors.CurrentDoor.name}");
                Doors.CurrentDoor.MoveToLinkedDoor();
            }
            else
            {
                Debug.LogWarning("[DoorInputManager] No valid door found.");
            }
        }
    }


}
