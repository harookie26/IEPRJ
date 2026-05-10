using Game.Level;
using UnityEngine;

public enum VerticalDoorDirection
{
    None,
    Up,
    Down
}

[FoldableInspector]
public class DoorsComponent : MonoBehaviour, IDoor
{
    [SerializeField] private DoorsComponent partnerDoor;

    private IRoom roomA;
    private IRoom roomB;

    private Collider triggerZone;
    private GameObject _player;

    // Vertical direction setting (Up/Down/None)
    [Header("Vertical Teleport Settings")]
    [SerializeField] private VerticalDoorDirection verticalDirection = VerticalDoorDirection.None;
    public VerticalDoorDirection VerticalDirection => verticalDirection;

    private static float _entryCooldown = 0.5f;
    private static float _lastEntryTime = -1f;
    private bool _playerInZone = false;

    public static DoorsComponent CurrentDoor;
    public int Id => GetInstanceID();
    public IRoom RoomA => roomA;
    public IRoom RoomB => roomB;

    [SerializeField] private float weight = 1f;
    public float Weight => weight;

    private UIManager uiManager;

    private void Awake()
    {
        if (partnerDoor == null)
        {
            Debug.LogError($"[Door:{name}] Missing partner door reference.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindFirstObjectByType<UIManager>();

        triggerZone = GetComponent<Collider>() ?? triggerZone;
    }

    private void OnDestroy()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInZone = true;
            CurrentDoor = this;
            ShowDoorHUD();
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && CurrentDoor == this)
        {
            _playerInZone = false;
            CurrentDoor = null;
            uiManager?.ClearForcedHUD();
        }
    }

    private void ShowDoorHUD()
    {
        if (uiManager == null) return;
        var doorInput = FindFirstObjectByType<DoorInputManager>();
        if (doorInput != null)
        {
            bool cooldownActive = Time.unscaledTime < doorInput.LastDoorUseTime + doorInput.doorUseCooldown;
            if (cooldownActive)
            {
                uiManager.ClearForcedHUD();
                return;
            }
        }
        switch (verticalDirection)
        {
            case VerticalDoorDirection.Up:
                uiManager.ShowHUDForce(UIManager.Keys.StairUp);
                break;
            case VerticalDoorDirection.Down:
                uiManager.ShowHUDForce(UIManager.Keys.StairDown);
                break;
            case VerticalDoorDirection.None:
            default:
                uiManager.ClearForcedHUD();
                break;
        }
    }

    public bool IsReadyToUse()
    {
        if (_player == null) return false;
        if (Time.time - _lastEntryTime < _entryCooldown) return false;

        return _playerInZone;
    }

    public void MoveToLinkedDoor()
    {
        if (partnerDoor == null)
        {
            Debug.LogError($"[Door:{name}] Partner door not assigned.");
            return;
        }

        Vector3 targetPos = partnerDoor.transform.position;
        _player.transform.position = targetPos;
        _lastEntryTime = Time.time;
    }

    public Vector3 GetEntryPointFor(IRoom fromRoom)
    {
        Vector3 offset = Vector3.zero;

        if (fromRoom == RoomA)
            offset = (RoomB.Center - RoomA.Center);
        else if (fromRoom == RoomB)
            offset = (RoomA.Center - RoomB.Center);
        else
            Debug.LogWarning($"[Door:{name}] Room {fromRoom?.Id} is not connected to this door.");

        offset = offset.normalized * 0.5f;
        return transform.position + offset;
    }
}