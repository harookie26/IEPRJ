using UnityEngine;
using System.Linq;
using Game.ObjectTypes;
using Game.Level;

[FoldableInspector]
public class DoorsComponent : MonoBehaviour, ILinkable, IDoor
{
    [SerializeField] private int roomAId;
    [SerializeField] private int roomBId;

    private IRoom roomA;
    private IRoom roomB;

    [SerializeField] private string linkID;
    [SerializeField, HideInInspector] private string uniqueID = System.Guid.NewGuid().ToString();

    [SerializeField] private Collider triggerZone;
    private GameObject _player;

    private static float _entryCooldown = 0.5f;
    private static float _lastEntryTime = -1f;
    private bool _playerInZone = false;

    public static DoorsComponent CurrentDoor;
    public string LinkID => linkID;
    public string UniqueID => uniqueID;

    public int Id => GetInstanceID();
    public IRoom RoomA => roomA;
    public IRoom RoomB => roomB;

    [SerializeField] private float weight = 1f;
    public float Weight => weight;

    private void Awake()
    {
        if (string.IsNullOrEmpty(linkID))
        {
            Debug.LogError($"[Door:{name}] Missing LinkID.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        LinkRegistry.Register(this);

        triggerZone = GetComponent<Collider>() ?? triggerZone;

        roomA = RoomRegistry.GetRoom(roomAId);
        roomB = RoomRegistry.GetRoom(roomBId);

        if (roomA == null || roomB == null)
        {
            Debug.LogError($"[Door:{name}] Could not find rooms for IDs {roomAId} and/or {roomBId}");
        }
    }

    private void OnDestroy()
    {
        LinkRegistry.Unregister(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInZone = true;
            CurrentDoor = this;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && CurrentDoor == this)
        {
            _playerInZone = false;
            CurrentDoor = null;
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
        var linkedList = LinkRegistry
            .GetLinkedObjects(linkID)
            .OfType<DoorsComponent>()
            .ToList();

        if (linkedList.Count < 2)
        {
            Debug.LogError($"[Door:{name}] Not enough linked doors for LinkID '{linkID}'");
            return;
        }

        var linked = linkedList
            .OrderByDescending(d => Vector3.Distance(d.transform.position, transform.position))
            .First();

        if (linked == this)
        {
            Debug.LogError($"[Door:{name}] Only found self as link target.");
            return;
        }

        Vector3 targetPos = linked.transform.position;
        Vector3 playerPos = _player.transform.position;

        _player.transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
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