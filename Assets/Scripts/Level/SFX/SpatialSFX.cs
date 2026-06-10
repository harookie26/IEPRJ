using Game.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[FoldableInspector]

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SphereCollider))]
public class SpatialSFX : MonoBehaviour, ISaveable
{
    public enum DetectionMode
    {
        Player,
        Enemy,
        Both
    }

    private static readonly Dictionary<string, SpatialSFX> Registry = new();

    [Header("Identification")]
    [SerializeField] private string uniqueID;

    [Header("Detection")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.Player;

    [SerializeField] private bool useTriggerRadius = true;
    [SerializeField] private float triggerRadius = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip clip;

    [SerializeField] private bool isLooping = true;
    [SerializeField] private float delayBeforeLooping = 0f;

    [SerializeField] private bool playOnEnter = true;
    [SerializeField] private bool stopOnExit = true;

    [Header("3D Audio")]
    [Tooltip("Inside this range, sound stays at full volume")]
    [SerializeField] private float minDistance = 2f;

    [Tooltip("Beyond this range, sound is inaudible")]
    [SerializeField] private float maxDistance = 15f;

    [Tooltip("How sound volume decreases with distance")]
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    private AudioSource _audioSource;
    private Coroutine _loopRoutine;

    private bool _isActive = true;

    public string SaveKey => uniqueID;

    public string UniqueID => uniqueID;
    public bool IsActive => _isActive;
    public System.Type GetStateType() => typeof(SpatialSFXSaveData);

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _audioSource.clip = clip;
        _audioSource.loop = false;

        // 3D AUDIO SETTINGS
        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode = rolloffMode;

        _audioSource.minDistance = minDistance;
        _audioSource.maxDistance = maxDistance;

        SphereCollider col = GetComponent<SphereCollider>();

        col.isTrigger = true;
        col.radius = triggerRadius;
        col.center = Vector3.zero;

        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(uniqueID))
        {
            Debug.LogError($"SpatialSFX on {gameObject.name} has no unique ID.");
            return;
        }

        if (Registry.ContainsKey(uniqueID))
        {
            Debug.LogError($"Duplicate SpatialSFX ID detected: {uniqueID}");
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

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive)
            return;

        if (!IsValidTarget(other))
            return;

        if (playOnEnter)
        {
            StartPlayback();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidTarget(other))
            return;

        if (stopOnExit)
        {
            StopPlayback();
        }
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

    private void StartPlayback()
    {
        if (_loopRoutine != null)
            return;

        _loopRoutine = StartCoroutine(LoopRoutine());
    }

    private IEnumerator LoopRoutine()
    {
        do
        {
            if (!_isActive)
            {
                StopPlayback();
                yield break;
            }

            _audioSource.Play();

            yield return new WaitForSeconds(_audioSource.clip.length);

            if (!isLooping)
                break;

            if (delayBeforeLooping > 0f)
                yield return new WaitForSeconds(delayBeforeLooping);

        } while (isLooping);

        _loopRoutine = null;
    }

    private void StopPlayback()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }

        _audioSource.Stop();
    }

    // =========================
    // GLOBAL CONTROL API
    // =========================

    public static void Activate(string id)
    {
        if (Registry.TryGetValue(id, out SpatialSFX sfx))
        {
            sfx._isActive = true;
        }
    }

    public static void Deactivate(string id)
    {
        if (Registry.TryGetValue(id, out SpatialSFX sfx))
        {
            sfx._isActive = false;
            sfx.StopPlayback();
        }
    }

    public static void Toggle(string id, bool state)
    {
        if (state)
            Activate(id);
        else
            Deactivate(id);
    }

    public static SpatialSFX Get(string id)
    {
        Registry.TryGetValue(id, out SpatialSFX sfx);
        return sfx;
    }

    public void SetActiveState(bool state)
    {
        _isActive = state;

        if (!_isActive)
        {
            StopPlayback();
        }
    }


    // =========================
    // GLOBAL SAVE STATE
    // =========================

    public object CaptureState()
    {
        return new SpatialSFXSaveData
        {
            id = uniqueID,
            isActive = _isActive
        };
    }

    public void RestoreState(object state)
    {
        var data = (SpatialSFXSaveData)state;
        SetActiveState(data.isActive);

        if (data.isActive)
            Debug.Log($"[Global Save System] State restored for SpatialSFX: {data.id}");
    }

    private void OnDrawGizmosSelected()
    {
        // Trigger distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // 3D audio range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }


}