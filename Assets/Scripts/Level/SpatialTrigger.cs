using Game.States;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SpatialTrigger : MonoBehaviour, ISaveable
{
    public enum DetectionMode
    {
        Player,
        Enemy,
        Both
    }

    private static readonly Dictionary<string, SpatialTrigger> Registry = new();

    [Header("Identification")]
    [SerializeField] private string uniqueID;

    [Header("Detection")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.Player;

    [SerializeField] private float triggerRadius = 5f;

    [SerializeField] private bool triggerOnce = true;

    private bool _isActive = true;

    private SpatialTriggerAction[] _actions;

    private readonly HashSet<GameObject> _inside = new();

    public string SaveKey => uniqueID;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();

        col.isTrigger = true;
        col.radius = triggerRadius;
        col.center = Vector3.zero;

        _actions = GetComponents<SpatialTriggerAction>();

        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void OnDisable()
    {
        CleanupAllTargets();
    }

    private bool IsValidTarget(Collider other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isEnemy = other.CompareTag("Enemy");

        return detectionMode switch
        {
            DetectionMode.Player => isPlayer,
            DetectionMode.Enemy => isEnemy,
            DetectionMode.Both => isPlayer || isEnemy,
            _ => false
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive)
            return;

        if (!IsValidTarget(other))
            return;

        foreach (var action in _actions)
        {
            _inside.Add(other.gameObject);
            action.OnEnter(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isActive)
            return;

        if (!IsValidTarget(other))
            return;

        foreach (var action in _actions)
        {
            _inside.Remove(other.gameObject);
            action.OnExit(other.gameObject);
        }
    }

    public void SetActiveState(bool state)
    {
        _isActive = state;
    }

    private void CleanupAllTargets()
    {
        foreach (var target in _inside)
        {
            if (target == null) continue;

            foreach (var action in _actions)
            {
                action.OnExit(target);
            }
        }

        _inside.Clear();
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(uniqueID))
        {
            Debug.LogError($"SpatialTrigger on {gameObject.name} has no unique ID.");
            return;
        }

        if (Registry.ContainsKey(uniqueID))
        {
            Debug.LogError($"Duplicate SpatialTrigger ID detected: {uniqueID}");
            return;
        }

        Registry.Add(uniqueID, this);
        GlobalSaveSystem.Register(this);
    }

    private void Unregister()
    {
        if (Registry.ContainsKey(uniqueID))
            Registry.Remove(uniqueID);

        GlobalSaveSystem.Unregister(this);
    }

    public object CaptureState()
    {
        return new SpatialTriggerSaveData
        {
            id = uniqueID,
            isActive = _isActive
        };
    }

    public void RestoreState(object state)
    {
        var data = (SpatialTriggerSaveData)state;

        _isActive = data.isActive;
    }

    // =========================
    // GLOBAL CONTROL API
    // =========================

    public static void Activate(string id)
    {
        if (Registry.TryGetValue(id, out SpatialTrigger stx))
        {
            stx._isActive = true;
        }
    }

    public static void Deactivate(string id)
    {
        if (Registry.TryGetValue(id, out SpatialTrigger stx))
        {
            stx._isActive = false;
        }
    }

    public static void Toggle(string id, bool state)
    {
        if (state)
            Activate(id);
        else
            Deactivate(id);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
