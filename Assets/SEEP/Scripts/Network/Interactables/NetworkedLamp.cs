using FishNet.Broadcast;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;

namespace SEEP.Network.Interactables
{
    public class NetworkedLamp : BroadcastInteractable<LampState>
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

        public void Interact()
        {
            Interact(null);
        }

        public override void Interact(InteractorController controller)
        {
            _isEnabled = !_isEnabled;
            base.Interact(controller);
        }

        protected override void UpdateState()
        {
            if (_isEnabled)
                _material.EnableKeyword("_EMISSION");
            else
                _material.DisableKeyword("_EMISSION");
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