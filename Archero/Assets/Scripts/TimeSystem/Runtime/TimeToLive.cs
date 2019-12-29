using System;
using Unity.Entities;

namespace General.Runtime
{
    [GenerateAuthoringComponent]
    public struct TimeToLive : IComponentData
    {
        public float Value;
    }
}