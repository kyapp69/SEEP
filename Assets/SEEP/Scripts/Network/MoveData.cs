using FishNet.Object.Prediction;

namespace SEEP.Network
{
    public struct MoveData : IReplicateData
    {
        public float Throttle;
        public float Steering;
        public bool Jump;

        public MoveData(bool jump, float throttle, float steering)
        {
            Jump = jump;
            Throttle = throttle;
            Steering = steering;
            _tick = 0;
        }
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}