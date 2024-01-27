using FishNet;
using FishNet.Broadcast;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;

namespace SEEP.Network.Interactables
{
    [RequireComponent(typeof(Rigidbody))]
    public class BroadcastLift : BroadcastInteractable<LiftState>
    {
        [SerializeField] private Vector3 from = default, to = default;
        [SerializeField] private Transform relativeTo;
        [SerializeField, Min(0.01f)] private float duration = 1f;
        [SerializeField] private bool autoReverse = false, smoothstep = false;

        private Rigidbody _body;
        private float _value;

        private bool Reversed { get; set; }
        private bool AutoReverse
        {
            get => autoReverse;
            set => autoReverse = value;
        }

        private float SmoothedValue => 3f * _value * _value - 2f * _value * _value * _value;

        protected override void Awake()
        {
            base.Awake();
            _body = GetComponent<Rigidbody>();
            Reversed = false;
            InstanceFinder.TimeManager.OnTick += TimeManagerOnOnTick;
        }

        private void TimeManagerOnOnTick()
        {
            var delta = (float)InstanceFinder.TimeManager.TickDelta / duration;
            if (Reversed)
            {
                _value -= delta;
                if (_value <= 0f)
                {
                    if (autoReverse)
                    {
                        _value = Mathf.Min(1f, -_value);
                        Reversed = false;
                    }
                    else
                    {
                        _value = 0f;
                        enabled = false;
                    }
                }
            }
            else
            {
                _value += delta;
                if (_value >= 1f)
                {
                    if (autoReverse)
                    {
                        _value = Mathf.Max(0f, 2f - _value);
                        Reversed = true;
                    }
                    else
                    {
                        _value = 1f;
                        enabled = false;
                    }
                }
            }

            Interpolate(smoothstep ? SmoothedValue : _value);
        }
        
        public override void Interact(InteractorController controller)
        {
            //_body.DOMove(to, duration, false);
            Reversed = !Reversed;
            base.Interact(controller);
        }

        private void Interpolate(float t)
        {
            var pos = relativeTo
                ? Vector3.LerpUnclamped(relativeTo.TransformPoint(from), relativeTo.TransformPoint(to), t)
                : Vector3.LerpUnclamped(from, to, t);

            _body.MovePosition(pos);
        }

        protected override LiftState GetState()
        {
            return new LiftState
            {
                Reversed = Reversed,
                AutomaticReversed = AutoReverse,
                StartPosition = from,
                IsSmoothed = smoothstep,
                EndPosition = to
            };
        }

        protected override void SetState(LiftState state)
        {
            from = state.StartPosition;
            Reversed = state.Reversed;
            smoothstep = state.IsSmoothed;
            AutoReverse = state.AutomaticReversed;
            to = state.EndPosition;
        }

        protected override void UpdateState()
        {
        }
    }

    public struct LiftState : IBroadcast, IIdentifcable
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public bool Reversed;
        public bool AutomaticReversed;
        public bool IsSmoothed;
        public int ID { get; set; }
    }
}