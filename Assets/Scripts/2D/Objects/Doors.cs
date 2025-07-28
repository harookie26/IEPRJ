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

    private void Start()
    {
        if (string.IsNullOrEmpty(linkID))
        {
            Debug.LogError($"[Door:{name}] Missing LinkID.");
            return;
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        LinkRegistry.Register(this);
    }

    private void OnDestroy()
    {
        LinkRegistry.Unregister(this);
    }

    private bool CanMoveToDoor()
    {
        if (_player == null) return false;
        if (Time.time - _lastEntryTime < _entryCooldown) return false;

        return Mathf.Abs(_player.transform.position.x - transform.position.x) <= _graceDistance;
    }

    private void MoveToLinkedDoor()
    {
        var linked = LinkRegistry
            .GetLinkedObjects(linkID)
            .Where(d => d.UniqueID != this.UniqueID)
            .FirstOrDefault();

        if (linked is not Doors target)
        {
            Debug.LogWarning($"[Door:{name}] No linked door found for ID '{linkID}'");
            return;
        }

        Vector3 targetPos = target.transform.position;
        Vector3 playerPos = _player.transform.position;
        _player.transform.position = new Vector3(targetPos.x, targetPos.y, playerPos.z);
        _lastEntryTime = Time.time;

        Debug.Log($"[Door:{name}] Teleported to {target.name}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && CanMoveToDoor())
            MoveToLinkedDoor();
    }
}
