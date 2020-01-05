using Unity.Entities;

namespace NavJob.Systems
{
    [GenerateAuthoringComponent]
    public struct LockRotation : IComponentData
    {
        public bool X;
        public bool Y;
        public bool Z;
    }
}