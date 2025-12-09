using UnityEngine;

[FoldableInspector]
public class PBFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followSpeed = 5f;

    private Transform _player;
    private Vector3 _lastPlayerPosition;
    private int _side = -1;

    private Rigidbody _rb;

    private Collider[] _colliders;
    private bool[] _colliderEnabledStates;
    private bool _originalIsKinematic;
    private bool _originalDetectCollisions;
    private RigidbodyConstraints _originalConstraints;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _colliders = GetComponentsInChildren<Collider>();
        _colliderEnabledStates = new bool[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++)
            _colliderEnabledStates[i] = _colliders[i].enabled;

        if (_rb != null)
        {
            _originalIsKinematic = _rb.isKinematic;
            _originalDetectCollisions = _rb.detectCollisions;
            _originalConstraints = _rb.constraints;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _lastPlayerPosition = _player.position;
        }
        else
            Debug.LogError("Player object with tag 'Player' not found in the scene.");

        if (_rb != null)
            _rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = false;
        }
    }

    private void OnDisable()
    {
        if (_rb != null)
        {
            _rb.isKinematic = _originalIsKinematic;
            _rb.detectCollisions = _originalDetectCollisions;
            _rb.constraints = _originalConstraints;
            _rb.angularVelocity = Vector3.zero;
        }

        if (_colliders != null && _colliderEnabledStates != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = _colliderEnabledStates[i];
            }
        }
    }

    private void Update()
    {
        if (_player == null) return;

        float deltaX = _player.position.x - _lastPlayerPosition.x;
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            int newSide = deltaX > 0 ? -1 : 1;

            if (newSide != _side)
            {
                _side = newSide;
                offset.x = Mathf.Abs(offset.x) * _side;
            }
        }

        Vector3 targetPosition = _player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        _lastPlayerPosition = _player.position;
    }
}