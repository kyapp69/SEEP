using FishNet.Object.Prediction;
using UnityEngine;

namespace SEEP.Network.Controllers
{
    public struct DroneMoveData : IReplicateData
    {
        public bool TeleportNeeded;
        public Vector2 PlayerInput;
        public Vector3 Forward, Right;
        public bool Jump;
        public bool DesiredToPush;

        public DroneMoveData(Vector2 playerInput, bool isJump, bool desiredToPush, Vector3 rightAxis, Vector3 forwardAxis, bool isTeleportNeeded)
        {
            PlayerInput = playerInput;
            Forward = forwardAxis;
            Jump = isJump;
            DesiredToPush = desiredToPush;
            Right = rightAxis;
            TeleportNeeded = isTeleportNeeded;
            _tick = 0;
        }

        private uint _tick;

        public void Dispose()
        {
        }

        public uint GetTick()
        {
            return _tick;
        }

        public void SetTick(uint value)
        {
            _tick = value;
        }
    }
}