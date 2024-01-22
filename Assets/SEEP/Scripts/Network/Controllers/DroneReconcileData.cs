using FishNet.Object.Prediction;
using UnityEngine;

namespace SEEP.Network.Controllers
{
    public struct DroneReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}