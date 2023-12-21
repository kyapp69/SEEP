using FishNet.Object.Prediction;
using System;

namespace SEEP.Network.Controllers
{
    public struct DroneMoveData : IReplicateData
    {
        public float TargetAngle;
        public bool Jump;
        public bool IsMoving;

        //TODO: Rework move data. Need to add acceleration force support
        public DroneMoveData(float targetAngle, bool isMoving, bool jump)
        {
            TargetAngle = targetAngle;
            Jump = jump;
            IsMoving = isMoving;
            _tick = 0;
        }
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}