using System;
using Unity.Entities;

namespace Scripts.ECS.RuntimeComponents
{
    [GenerateAuthoringComponent]
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }
}