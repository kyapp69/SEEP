using DG.Tweening;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SEEP.Network.Controllers;
using SEEP.Offline.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace SEEP.Network.Interactables
{
    public class NetworkLift : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string message;
        [SerializeField] private Vector3 from = default, to = default;
        [SerializeField, Min(0.01f)] private float duration = 1f;

        private string _message;
        private InteractableType _type;
        private Rigidbody _rigidbody;
        private float _value;

        [SyncVar] private bool _toEnd;
        [SyncVar] private bool _isEnabled;
        
        private float SmoothedValue => 3f * _value * _value - 2f * _value * _value * _value;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _message = message != "" ? message : "null";
            _type = InteractableType.Object;
            InstanceFinder.TimeManager.OnTick += TimeManagerOnOnTick;
        }

        public InteractableType GetInteractableType() => _type;

        public string GetMessage() => _message;

        public void Interact(InteractorController interactor)
        {
            GiveOwnership(interactor.GetComponent<NetworkBehaviour>().Owner);
            CmdInvokeEvent();
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdInvokeEvent()
        {
            _toEnd = !_toEnd;
            StartMovement(_toEnd);
            RpcInvokeEvent();
        }

        [ObserversRpc]
        private void RpcInvokeEvent()
        {
            if (!IsClientOnly) return;
            StartMovement(_toEnd);
        }

        private void StartMovement(bool toEnd)
        {
            if (toEnd)
            {
                _rigidbody.DOMove(to, duration);
            }
            else
            {
                _rigidbody.DOMove(from, duration);
            }
        }

        private void MoveLift(float value)
        {
            var pos = Vector3.LerpUnclamped(from, to, value);

            _rigidbody.MovePosition(pos);
        }

        private void TimeManagerOnOnTick()
        {
            if (!_isEnabled) return;
            var delta = (float)InstanceFinder.TimeManager.TickDelta / duration;
            if (_toEnd)
            {
                _value -= delta;
                if (_value <= 0f)
                {
                    _value = 0f;
                    if (IsServer)
                        _isEnabled = false;
                }
            }
            else
            {
                _value += delta;
                if (_value >= 1f)
                {
                    _value = 1f;
                    if (IsServer)
                        _isEnabled = false;
                }
            }

            MoveLift(SmoothedValue);
        }
    }
}