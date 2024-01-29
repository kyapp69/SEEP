using FishNet;
using FishNet.Broadcast;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;
using Logger = SEEP.Utils.Logger;

namespace SEEP.Network.Interactables
{
    public class BroadcastLamp : BroadcastInteractable<LampState>
    {
        private Material _material;
        private bool _isEnabled;

        protected override void Awake()
        {
            _isEnabled = false;
            _material = GetComponent<MeshRenderer>().material;
            ForceState = true;
            UpdateState();
            base.Awake();
        }

        public override void Interact(InteractorController controller)
        {
            _isEnabled = !_isEnabled;
            base.Interact(controller);
        }

        protected override void UpdateState()
        {
            _material.color = _isEnabled ? Color.yellow : Color.black;
        }

        protected override LampState GetState()
        {
            return new LampState
            {
                IsEnabled = _isEnabled,
            };
        }

        protected override void SetState(LampState state)
        {
            _isEnabled = state.IsEnabled;
            UpdateState();
        }
    }

    public struct LampState : IBroadcast, IIdentifcable
    {
        public bool IsEnabled;
        public int ID { get; set; }
    }
}