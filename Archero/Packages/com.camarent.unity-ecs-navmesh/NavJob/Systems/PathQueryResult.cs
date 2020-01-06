using Unity.Entities;
using Unity.Mathematics;

namespace NavJob.Systems
{
    public struct PathQueryResult : IComponentData
    {
        public NavAgentQuerySystem.PathStatus status;
        public int waypointIndex;
        public float3 nextWaypoint;
    }
}