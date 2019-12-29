using Unity.Entities;

namespace General.Runtime
{
    [GenerateAuthoringComponent]
    public struct Damage : IComponentData
    {
        public float Value;
    }
}