using UnityEngine;
using System.Linq;
using Game.ObjectTypes;

public class Doors : MonoBehaviour, ILinkable
{
    [SerializeField] private string linkID;
    [SerializeField, HideInInspector] private string uniqueID = System.Guid.NewGuid().ToString();

    private float _graceDistance = 0.4f;
    private GameObject _player;
    private static float _entryCooldown = 0.5f;
    private static float _lastEntryTime = -1f;

    public string LinkID => linkID;
    public string UniqueID => uniqueID;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        var linked = LinkRegistry
            .GetLinkedObjects(LinkID)
            .Where(d => d.UniqueID != this.UniqueID)
            .ToList();

        foreach (var target in linked)
        {
            if (target is Doors other)
            {
                Gizmos.DrawLine(transform.position, other.transform.position);
                Gizmos.DrawSphere(other.transform.position, 0.1f);
            }
        }
    }
#endif


    private void Start()
    {

        if (string.IsNullOrEmpty(linkID))
        {
            Debug.LogError($"[Door:{name}] Missing LinkID.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        LinkRegistry.Register(this);

        Debug.Log($"[INIT] {gameObject.name} | LinkID: {linkID} | UID: {uniqueID}");

    }

    private void OnDestroy()
    {
        LinkRegistry.Unregister(this);
    }

    private bool CanMoveToDoor()
    {
        if (_player == null) return false;
        if (Time.time - _lastEntryTime < _entryCooldown) return false;

        // Iterate over linked objects and log their info
        foreach (var d in LinkRegistry.GetLinkedObjects(linkID))
        {
            if (d is Component comp)
                Debug.Log($"Linked: {comp.gameObject.name} | ID: {d.LinkID} | UID: {d.UniqueID}");
            else
                Debug.Log($"Linked: [no gameObject] | ID: {d.LinkID} | UID: {d.UniqueID}");
        }

        return Mathf.Abs(_player.transform.position.x - transform.position.x) <= _graceDistance;
    }

    private void MoveToLinkedDoor()
    {
        var linked = LinkRegistry
        .GetLinkedObjects(linkID)
        .OfType<Doors>()
        .FirstOrDefault(d => d.UniqueID != this.UniqueID);

        if (linked == null)
        {
            Debug.LogError($"[Door:{name}] Linked object is not a Doors component.");
            return;
        }

        Vector3 targetPos = linked.transform.position;
        Vector3 playerPos = _player.transform.position;
        _player.transform.position = new Vector3(targetPos.x, targetPos.y, playerPos.z);
        _lastEntryTime = Time.time;

        Debug.Log($"[Door:{name}] Teleported to {linked.name} at {targetPos}");
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && CanMoveToDoor())
            MoveToLinkedDoor();
    }
}
