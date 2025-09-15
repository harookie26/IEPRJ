using UnityEngine;
using System.Linq;
using static EventNames;

public class DoorInputManager : MonoBehaviour
{
    private bool _isCutsceneActive = false;
    private GameObject _player;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, () => _isCutsceneActive = true);
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, () => _isCutsceneActive = false);
    }

    private void OnEnable()
    {
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (_isCutsceneActive || _player == null)
            return;
        
        if (InputManager.Instance.WasInteractPressed())
        {
            Debug.Log("[DoorInputManager] W key pressed");

            if (DoorsComponent.CurrentDoor?.IsReadyToUse() == true)
            {
                Debug.Log($"[DoorInputManager] Teleporting via {DoorsComponent.CurrentDoor.name}");
                DoorsComponent.CurrentDoor.MoveToLinkedDoor();
            }
            else
            {
                Debug.LogWarning("[DoorInputManager] No valid door found.");
            }
        }
    }


}
