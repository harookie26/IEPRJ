using UnityEngine;
using System;
using Game.States;

public class PlayerStateMachine : MonoBehaviour
{
    IPlayerState _currentState;

    public event Action HidingEntered;
    public event Action HidingExited;

    [Header("Stealth")]
    [Tooltip("Radius to search for nearby walls when stealth key is pressed")]
    public float stealthRadius = 2f;
    [Tooltip("Layer mask used to find walls for hiding")]
    public LayerMask stealthMask = ~0;
    [Tooltip("Small extra distance when raycasting towards the surface")]
    public float stealthRayExtra = 0.1f;

    [Tooltip("When true, prefer explicit WallHideAnchor components when selecting a hide point.")]
    public bool preferWallComponents = true;

    private float hideCooldown = 0.2f;
    private float hideCooldownTimer = 0f;

    void Start()
    {
        SetToDefaultState();
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnStealthPressed += HandleStealthPressed;
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnStealthPressed -= HandleStealthPressed;
    }

    void Update()
    {
        if (hideCooldownTimer > 0f)
            hideCooldownTimer -= Time.deltaTime;

        _currentState?.HandleInput();
        _currentState?.Tick();
    }

    public void SetState(IPlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    // Updated: now can auto-find a wall if none provided.
    public void RequestHide(Vector3? wallPoint = null, Vector3? wallNormal = null, bool autoFindIfMissing = true)
    {
        if (hideCooldownTimer > 0f)
            return;

        Vector3 wp = wallPoint ?? Vector3.zero;
        Vector3 wn = wallNormal ?? Vector3.zero;
        bool haveWallInfo = wallPoint.HasValue && wallNormal.HasValue;

        if (!haveWallInfo && autoFindIfMissing)
        {
            if (TryComputeHideAnchor(out Vector3 pos, out Vector3 norm))
            {
                wp = pos;
                wn = norm;
                haveWallInfo = true;
            }
        }

        SetState(new PlayerHidingState(this,
            haveWallInfo ? wp : (Vector3?)null,
            haveWallInfo ? wn : (Vector3?)null));
    }

    public void SetToDefaultState()
    {
        SetState(new DefaultState(this));
        hideCooldownTimer = hideCooldown;
    }

    public void NotifyHidingEntered() => HidingEntered?.Invoke();
    public void NotifyHidingExited() => HidingExited?.Invoke();
    public bool IsHiding => _currentState is PlayerHidingState;

    void HandleStealthPressed()
    {
        if (IsHiding)
        {
            SetToDefaultState();
            return;
        }

        if (hideCooldownTimer > 0f)
            return;

        RequestHide(); // will auto-find
    }

    // Unified computation of nearest hide anchor. Returns true if a good point found.
    bool TryComputeHideAnchor(out Vector3 bestPoint, out Vector3 bestNormal)
    {
        Vector3 origin = transform.position;
        bestPoint = default;
        bestNormal = default;

        // 1) Prefer explicit components
        if (preferWallComponents)
        {
            var anchors = FindObjectsOfType<WallHideAnchor>();
            float bestAnchorDistSqr = float.MaxValue;
            Vector3 bestAnchorPos = Vector3.zero;
            Vector3 bestAnchorNormal = Vector3.up;
            bool foundAnchor = false;

            foreach (var a in anchors)
            {
                float dsq = (a.transform.position - origin).sqrMagnitude;
                if (dsq <= stealthRadius * stealthRadius && dsq < bestAnchorDistSqr)
                {
                    if (a.TryGetAnchor(origin, out Vector3 pos, out Vector3 normal))
                    {
                        bestAnchorDistSqr = dsq;
                        bestAnchorPos = pos;
                        bestAnchorNormal = normal;
                        foundAnchor = true;
                    }
                }
            }

            if (foundAnchor)
            {
                bestPoint = bestAnchorPos;
                bestNormal = bestAnchorNormal;
                Debug.Log($"PlayerStateMachine: Stealth resolved via WallHideAnchor at {bestPoint} normal {bestNormal}");
                return true;
            }
        }

        // 2) Collider-based search
        Collider[] cols = Physics.OverlapSphere(origin, stealthRadius, stealthMask, QueryTriggerInteraction.Ignore);
        if (cols == null || cols.Length == 0)
            return false;

        float bestDistSq = float.MaxValue;
        bool found = false;

        foreach (var col in cols)
        {
            Vector3 closest = col.ClosestPoint(origin);
            Vector3 dir = closest - origin;
            float dist = dir.magnitude;
            if (dist <= 0.0001f)
            {
                dir = (origin - col.transform.position).normalized;
                if (dir.sqrMagnitude <= 0.0001f) dir = transform.forward;
                dist = Mathf.Max(0.01f, Vector3.Distance(origin, closest));
            }

            RaycastHit hit;
            if (Physics.Raycast(origin, dir.normalized, out hit, dist + stealthRayExtra, stealthMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == col || hit.collider.transform.IsChildOf(col.transform) || col.transform.IsChildOf(hit.collider.transform))
                {
                    float dsq = (hit.point - origin).sqrMagnitude;
                    if (dsq < bestDistSq)
                    {
                        bestDistSq = dsq;
                        bestPoint = hit.point;
                        bestNormal = hit.normal;
                        found = true;
                    }
                }
            }
            else
            {
                float dsq = (closest - origin).sqrMagnitude;
                if (dsq < bestDistSq)
                {
                    bestDistSq = dsq;
                    bestPoint = closest;
                    bestNormal = (closest - origin).normalized;
                    found = true;
                }
            }
        }

        if (found)
        {
            Debug.Log($"PlayerStateMachine: Stealth resolved at {bestPoint} normal {bestNormal}");
            return true;
        }

        return false;
    }

    class DefaultState : IPlayerState
    {
        readonly PlayerStateMachine _owner;
        public DefaultState(PlayerStateMachine owner) => _owner = owner;

        public void Enter() => Debug.Log("PlayerStateMachine: Enter DefaultState");
        public void Exit() => Debug.Log("PlayerStateMachine: Exit DefaultState");

        public void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Now uses the unified auto-find path.
                _owner.RequestHide();
            }
        }

        public void Tick() { }
    }
}