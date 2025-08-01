using UnityEngine;
using System.Linq;
using Game.ObjectTypes;
using Game.Level;

public class Doors : MonoBehaviour, ILinkable, IDoor
{
    [SerializeField] private int roomAId;
    [SerializeField] private int roomBId;

    private IRoom roomA;
    private IRoom roomB;

    [SerializeField] private string linkID;
    [SerializeField, HideInInspector] private string uniqueID = System.Guid.NewGuid().ToString();

    [SerializeField] private PolygonCollider2D triggerZone;
    private GameObject _player;

    private static float _entryCooldown = 0.5f;
    private static float _lastEntryTime = -1f;
    private bool _playerInZone = false;

    public static Doors CurrentDoor;
    public string LinkID => linkID;
    public string UniqueID => uniqueID;

    // IDoor implementation
    public int Id => GetInstanceID(); // Or use a serialized int if needed
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

        triggerZone = GetComponent<PolygonCollider2D>() ?? triggerZone;

        roomA = RoomRegistry.GetRoom(roomAId);
        roomB = RoomRegistry.GetRoom(roomBId);

        // Debug.Log($"[Door:{name}] connects Room {roomAId} and Room {roomBId}");

        if (roomA == null || roomB == null)
        {
            Debug.LogError($"[Door:{name}] Could not find rooms for IDs {roomAId} and/or {roomBId}");
        }
        else
        {
            if (roomA is RoomComponent rcA) rcA.AddDoor(this);
            if (roomB is RoomComponent rcB) rcB.AddDoor(this);
        }

        // Debug.Log($"[Door:{name}] Connected Room {roomA?.Id} <--> Room {roomB?.Id} (LinkID: {linkID})");

    }

    private void OnDestroy()
    {
        LinkRegistry.Unregister(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInZone = true;
            CurrentDoor = this;
        }

        if (other.CompareTag("Enemy"))
        {
            var ai = other.GetComponent<EnemyAI>();
            if (ai == null) return;

            if (ai.CurrentDoorTarget == this && ai.CanTeleportFrom(this))
            {
                MoveToPathDoor(ai);
            }
        }
    }
    void OnTriggerExit2D(Collider2D other)
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
            .OfType<Doors>()
            .ToList();

        if (linkedList.Count < 2)
        {
            Debug.LogError($"[Door:{name}] Not enough linked doors for LinkID '{linkID}'");
            return;
        }

        // Pick the farthest one to avoid teleporting to self
        var linked = linkedList
            .OrderByDescending(d => Vector2.Distance(d.transform.position, transform.position))
            .First();

        if (linked == this)
        {
            Debug.LogError($"[Door:{name}] Only found self as link target.");
            return;
        }

        Vector3 targetPos = linked.transform.position;
        Vector3 playerPos = _player.transform.position;

        _player.transform.position = new Vector3(targetPos.x, targetPos.y, playerPos.z);
        _lastEntryTime = Time.time;
    }

    private void MoveToPathDoor(EnemyAI ai)
    {
        if (!ai.CanTeleportFrom(this))
        {
            Debug.Log($"[Doors:{name}] Enemy teleport blocked by cooldown.");
            return;
        }

        if (ai.CurrentDoorTarget != this)
        {
            Debug.Log($"[Doors:{name}] Enemy tried to use wrong door. Expected: {ai.CurrentDoorTarget?.name}, Got: {name}");
            return;
        }

        var fromRoom = ai.CurrentRoom;
        var toRoom = RoomA == fromRoom ? RoomB : RoomA;

        if (toRoom == null)
        {
            Debug.LogWarning($"[Doors:{name}] Cannot resolve target room.");
            return;
        }

        var destination = LinkRegistry
            .GetLinkedObjects(linkID)
            .OfType<Doors>()
            .FirstOrDefault(d => d != this && (d.RoomA == toRoom || d.RoomB == toRoom));

        if (destination == null)
        {
            Debug.LogError($"[Doors:{name}] No linked door in Room {toRoom.Id}");
            return;
        }

        // Teleport and notify AI
        ai.transform.position = destination.transform.position;
        ai.RegisterTeleport(destination, toRoom);

        Debug.Log($"[Doors:{name}] Enemy teleported to Room {toRoom.Id} via {destination.name}");
    }


    public Vector2 GetEntryPointFor(IRoom fromRoom)
    {
        Vector2 offset = Vector2.zero;

        if (fromRoom == RoomA)
            offset = (RoomB.Center - RoomA.Center).normalized * 0.5f;
        else if (fromRoom == RoomB)
            offset = (RoomA.Center - RoomB.Center).normalized * 0.5f;
        else
            Debug.LogWarning($"[Door:{name}] Room {fromRoom?.Id} is not connected to this door.");

        return transform.position + (Vector3)offset;
    }

}
