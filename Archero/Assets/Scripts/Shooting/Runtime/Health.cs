using System;
using Unity.Entities;

namespace General.Runtime
{
    [GenerateAuthoringComponent]
    public struct Health : IComponentData
    {
        public float Value;
    }
}