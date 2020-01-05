using Unity.Entities;
using Unity.Mathematics;

public struct NavAgentTarget : IComponentData
{
    public bool lookToTarget;
    public bool needsToProcess;
    public float3 TargetPosition;
}