using UnityEngine;
using System.Linq;
using Game.ObjectTypes;

public class Doors : MonoBehaviour, ILinkable
{
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

    private void Start()
    {
        if (string.IsNullOrEmpty(linkID))
        {
            Debug.LogError($"[Door:{name}] Missing LinkID.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        LinkRegistry.Register(this);

        triggerZone = GetComponent<PolygonCollider2D>() ?? triggerZone;
    }

    private void OnDestroy()
    {
        LinkRegistry.Unregister(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            CurrentDoor = this;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && CurrentDoor == this)
            CurrentDoor = null;
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
}
