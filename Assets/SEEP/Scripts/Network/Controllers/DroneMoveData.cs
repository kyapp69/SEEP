using FishNet.Object.Prediction;
using UnityEngine;

namespace SEEP.Network.Controllers
{
    public struct DroneMoveData : IReplicateData
    {
        public Vector2 PlayerInput;
        public Vector3 Forward, Right;
        public bool Jump;

        public DroneMoveData(Vector2 playerInput, bool isJump, Vector3 rightAxis, Vector3 forwardAxis)
        {
            PlayerInput = playerInput;
            Forward = forwardAxis;
            Jump = isJump;
            Right = rightAxis;
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