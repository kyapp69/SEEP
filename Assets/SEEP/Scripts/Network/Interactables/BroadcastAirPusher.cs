using System;
using FishNet;
using FishNet.Broadcast;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;

namespace SEEP.Network.Interactables
{
    [RequireComponent(typeof(BoxCollider))]
    public class BroadcastAirPusher : BroadcastInteractable<AirPusherState>
    {
        [SerializeField] private Vector3 pushDirection = Vector3.up;
        [SerializeField, Min(0f)] private float pushSpeed = 10f, acceleration = 10f;

        private Vector3 _pushDirection;
        private BoxCollider _collider;
        private bool _isEnabled;

        protected override void Awake()
        {
            _isEnabled = true;
            _pushDirection = pushDirection;
            _collider = GetComponent<BoxCollider>();
            base.Awake();
        }

        private void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body&& _isEnabled)
            {
                //pushableObject.OnPush();
                Accelerate(body);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body && _isEnabled)
            {
                Accelerate(body);
            }
        }

        private void Accelerate(Rigidbody body)
        {
            if (body.TryGetComponent(out DroneController drone))
            {
                drone.PreventSnapToGround();
            }

            var velocity = body.velocity;
            if (velocity.y >= pushSpeed)
            {
                return;
            }

            if (acceleration > 0f)
            {
                velocity.y = Mathf.MoveTowards(velocity.y, pushSpeed, acceleration * (float)InstanceFinder.TimeManager.TickDelta);
            }
            else
            {
                velocity.y = pushSpeed;
            }

            body.velocity = velocity;
        }


        public override void Interact(InteractorController controller)
        {
            base.Interact(controller);
        }

        protected override AirPusherState GetState()
        {
            return new AirPusherState
            {
                IsEnabled = _isEnabled,
                PushDirection = _pushDirection
            };
        }

        protected override void SetState(AirPusherState state)
        {
            _isEnabled = state.IsEnabled;
            _pushDirection = state.PushDirection;
        }

        protected override void UpdateState()
        {
        }
    }

    public struct AirPusherState : IBroadcast, IIdentifcable
    {
        public Vector3 PushDirection;
        public bool IsEnabled;
        public int ID { get; set; }
    }
}