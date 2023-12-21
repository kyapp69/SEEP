using FishNet.Object.Prediction;
using UnityEngine;

namespace SEEP.Network.Controllers
{
    public struct DroneReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public DroneReconcileData(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            Position = position;
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