using Game.Level;
using UnityEngine;

public enum VerticalDoorDirection
{
    None,
    Up,
    Down
}

[FoldableInspector]
public class StairsComponent : MonoBehaviour, IStair
{
    [SerializeField] private StairsComponent partnerDoor;

    private IRoom roomA;
    private IRoom roomB;

    private Collider triggerZone;
    private GameObject _player;

    // Vertical direction setting (Up/Down/None)
    [Header("Vertical Teleport Settings")]
    [SerializeField] private VerticalDoorDirection verticalDirection = VerticalDoorDirection.None;
    public VerticalDoorDirection VerticalDirection => verticalDirection;

    private bool _playerInZone = false;

    public bool isInaccesibleOnGameStart = false;

    public static StairsComponent CurrentDoor;
    public int Id => GetInstanceID();
    public IRoom RoomA => roomA;
    public IRoom RoomB => roomB;

    [SerializeField] private float weight = 1f;
    public float Weight => weight;

    private UIManager uiManager;
    private StairsInputManager _doorInputManager;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (partnerDoor == null)
        {
            Debug.LogError($"[Door:{name}] Missing partner door reference.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindFirstObjectByType<UIManager>();
        _doorInputManager = FindFirstObjectByType<StairsInputManager>();
        _playerMovement = FindFirstObjectByType<PlayerMovement>();

        triggerZone = GetComponent<Collider>() ?? triggerZone;

    }

    private void Update()
    {
        if (isInaccesibleOnGameStart)
        {
            if (PlayerCollectibleManager.Instance.HasCollected("Paintbucket"))
            {
                isInaccesibleOnGameStart = false;
            }
        }
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

        if (_doorInputManager != null)
        {
            bool cooldownActive = Time.unscaledTime < _doorInputManager.LastDoorUseTime + _doorInputManager.doorUseCooldown;
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
        if (!_playerInZone)
        {
            Debug.Log($"[Stairs:{name}] Player not in zone.");
            return false;
        }

        if (_playerMovement == null)
        {
            Debug.LogError($"[Stairs:{name}] PlayerMovement reference is NULL!");
            return false;
        }

        return true;
    }

    public void MoveToLinkedDoor()
    {
        if (isInaccesibleOnGameStart)
        {
            DialogueTriggerManager.Instance.TriggerInaccessibleAreaDialogue();
            return;
        }

        if (partnerDoor == null)
        {
            Debug.LogError($"[Stairs:{name}] Partner door not assigned. Teleport failed.");
            return;
        }

        if (_player == null)
        {
            Debug.LogError($"[Stairs:{name}] Player reference is null. Teleport failed.");
            return;
        }

        Vector3 playerPosBefore = _player.transform.position;
        Vector3 targetPos = partnerDoor.transform.position;

        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.MovePosition(targetPos);
        }
        else
        {
            _player.transform.position = targetPos;
        }

        Vector3 playerPosAfter = _player.transform.position;
        Debug.Log($"  - Player position after move: {playerPosAfter}");
        Debug.Log($"  - Position changed: {playerPosBefore != playerPosAfter}");
        Debug.Log($"[Stairs:{name}] Teleported player from {playerPosBefore} to {playerPosAfter}");

        if (_playerMovement != null)
        {
            _playerMovement.ResetVelocity();
        }
        else
        {
            Debug.LogWarning($"[Stairs:{name}] PlayerMovement not found. Could not reset velocity.");
        }
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