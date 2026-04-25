using Game.States;
using System;
using UnityEngine;

namespace Assets.Scripts.Level.Paintbrush
{
    public class PaintbrushStateMachine: MonoBehaviour
    {
        IPaintbrushState _currentState;

        public enum PaintbrushStateKind { Unknown, Default, Channel }
        public PaintbrushStateKind CurrentState { get; private set; } = PaintbrushStateKind.Unknown;

        public Type CurrentStateType => _currentState?.GetType();
        public string CurrentStateName => _currentState?.GetType().Name ?? "None";

        public bool IsState<T>() where T : IPaintbrushState => _currentState is T;

        [Header("Channel")]
        [Tooltip("Hover height above ground while channeling")]
        public float channelHoverHeight = 0.3f;
        [Tooltip("Vertical bob amplitude while channeling")]
        public float channelBobAmplitude = 0.05f;
        [Tooltip("Bob frequency (cycles per second)")]
        public float channelBobFrequency = 2f;
        [Tooltip("How fast to move toward target hover height")]
        public float channelRiseLerpSpeed = 6f;
        [Tooltip("Max raycast distance downward to find ground while channeling")]
        public float channelGroundRayDistance = 5f;
        [Tooltip("Layer mask for ground detection while channeling")]
        public LayerMask channelGroundMask = ~0;

        [Header("Debug")]
        [Tooltip("Draw the channel ground ray while channeling")]
        public bool channelDrawGroundRay = true;

        void Start()
        {
            SetToDefaultState();
        }

        void Update()
        {
        }


        public void SetState(IPaintbrushState newState)
        {
            _currentState?.Exit();

            _currentState = newState;

            if (newState is ChannelState) CurrentState = PaintbrushStateKind.Channel;
            else if (newState is DefaultState) CurrentState = PaintbrushStateKind.Default;
            else CurrentState = PaintbrushStateKind.Unknown;

            _currentState?.Enter();
        }

        // Public API to toggle channel state
        public void EnterChannelState()
        {
            if (!(_currentState is ChannelState))
                SetState(new ChannelState(this));
        }

        public void ExitChannelState()
        {
            if (_currentState is ChannelState)
                SetToDefaultState();
        }

        public void SetToDefaultState()
        {
            SetState(new DefaultState(this));
        }

        public bool IsChanneling => _currentState is ChannelState;

        class DefaultState : IPaintbrushState
        {
            readonly PaintbrushStateMachine _owner;
            public DefaultState(PaintbrushStateMachine owner) => _owner = owner;

            public void Enter() => Debug.Log("PaintbrushStateMachine: Enter DefaultState");
            public void Exit() => Debug.Log("PaintbrushStateMachine: Exit DefaultState");

            public void HandleInput()
            {
            }

            public void Tick() { }
        }



        class ChannelState : IPaintbrushState
        {
            readonly PaintbrushStateMachine _owner;

            float _time;
            float _lastGroundY;
            Rigidbody _rb;
            bool _hadRb;
            bool _originalUseGravity;

            public ChannelState(PaintbrushStateMachine owner) => _owner = owner;

            public void Enter()
            {
                Debug.Log("PaintbrushStateMachine: Enter ChannelState");

                _rb = _owner.GetComponent<Rigidbody>();
                _hadRb = _rb != null;
                if (_hadRb)
                {
                    _originalUseGravity = _rb.useGravity;
                    _rb.useGravity = false;
                    _rb.linearVelocity = Vector3.zero;
                }

                _time = 0f;
                _lastGroundY = SampleGroundY(_owner.transform.position);
            }

            public void Exit()
            {
                Debug.Log("PaintbrushStateMachine: Exit ChannelState");

                if (_hadRb && _rb != null)
                {
                    _rb.useGravity = _originalUseGravity;
                }
            }

            public void HandleInput() { }

            public void Tick()
            {
                _time += Time.deltaTime;

                float groundY = SampleGroundY(_owner.transform.position);
                if (!float.IsNaN(groundY))
                    _lastGroundY = groundY;

                float bob = Mathf.Sin(_time * Mathf.PI * 2f * _owner.channelBobFrequency) * _owner.channelBobAmplitude;
                float targetY = _lastGroundY + _owner.channelHoverHeight + bob;

                Vector3 pos = _owner.transform.position;
                float newY = Mathf.Lerp(pos.y, targetY, Time.deltaTime * _owner.channelRiseLerpSpeed);
                Vector3 newPos = new Vector3(pos.x, newY, pos.z);

                if (_hadRb && _rb != null)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.MovePosition(newPos);
                }
                else
                {
                    _owner.transform.position = newPos;
                }
            }

            float SampleGroundY(Vector3 origin)
            {
                Ray ray = new Ray(origin + Vector3.up * 0.1f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, _owner.channelGroundRayDistance, _owner.channelGroundMask, QueryTriggerInteraction.Ignore))
                {
                    return hit.point.y;
                }

                return origin.y - _owner.channelHoverHeight;
            }
        }

        void OnDrawGizmos()
        {
            if (!channelDrawGroundRay || !IsChanneling) return;

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Vector3 dir = Vector3.down;
            float maxDist = channelGroundRayDistance;

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
            Gizmos.DrawRay(origin, dir * maxDist);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDist, channelGroundMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.05f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + dir * maxDist);
            }
        }

    }
}