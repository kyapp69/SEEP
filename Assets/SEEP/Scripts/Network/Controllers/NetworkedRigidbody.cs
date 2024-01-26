using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

namespace SEEP.Network.Controllers
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkedRigidbody : NetworkBehaviour
    {
        private Rigidbody _rigidbody;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _rigidbody = GetComponent<Rigidbody>();
            base.TimeManager.OnPostTick += TimeManager_OnTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.TimeManager.OnTick -= TimeManager_OnTick;
        }
        private void TimeManager_OnTick()
        {
            //Does not move using input, this is only a reactive object so pass in default to move.
            Move(default, false);
            //Send reconcile per usual.
            if (IsServer)
            {
                ReconcileData rd = new ReconcileData(transform.position, transform.rotation, _rigidbody.velocity, _rigidbody.angularVelocity);
                Reconciliation(rd, true);
            }
        }


        
        [Replicate]
        private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable,
            bool replaying = false){}


        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            transform.position = rd.Position;
            transform.rotation = rd.Rotation;
            _rigidbody.velocity = rd.Velocity;
            _rigidbody.angularVelocity = rd.AngularVelocity;
        }
    }

    public struct MoveData : IReplicateData
    {
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            _tick = 0;
        }
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}