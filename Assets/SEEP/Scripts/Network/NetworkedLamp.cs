using System;
using FishNet.Broadcast;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;
using Logger = SEEP.Utils.Logger;
using Random = UnityEngine.Random;

namespace SEEP.Network
{
    public class NetworkedLamp : NetworkedInteractiveObject<LampState>
    {
        private Material _material;
        private bool _isEnabled;
        private Color _emissionColor;

        protected override void Awake()
        {
            _isEnabled = false;
            _emissionColor = Color.white;
            _material = GetComponent<MeshRenderer>().material;
            ForceState = true;
            UpdateState();
            base.Awake();
        }

        public override void Interact(InteractorController controller)
        {
            _emissionColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            base.Interact(controller);
        }

        protected override void UpdateState()
        {
            _material.color = _emissionColor;
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
                EmissionColor = _emissionColor
            };
        }

        protected override void SetState(LampState state)
        {
            _isEnabled = state.IsEnabled;
            _emissionColor = state.EmissionColor;
            UpdateState();
        }
    }

    public struct LampState : IBroadcast, IIdentifcable
    {
        public bool IsEnabled;
        public Color EmissionColor;
        public int ID { get; set; }
    }
}