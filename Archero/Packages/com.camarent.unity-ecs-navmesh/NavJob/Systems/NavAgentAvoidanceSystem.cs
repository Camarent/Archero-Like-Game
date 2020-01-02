using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using NavJob.Components;

namespace NavJob.Systems
{
    //[DisableAutoCreation]
    public class NavAgentAvoidanceSystem : JobComponentSystem
    {

        public NativeMultiHashMap<int, int> IndexMap;
        public NativeMultiHashMap<int, float3> NextPositionMap;
        private NavMeshQuery _navMeshQuery;

        [BurstCompile]
        struct NavAgentAvoidanceJob : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            // This job goes last and should do the deallocation
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavAgent> Agents;
            //public ComponentDataFromEntity<NavAgentAvoidance> avoidances;

            [ReadOnly] public NativeMultiHashMap<int, int> indexMap;
            [ReadOnly] public NativeMultiHashMap<int, float3> nextPositionMap;
            [ReadOnly] public NavMeshQuery NavMeshQuery;
            public float DeltaTime;
            public void ExecuteFirst(int index) { }

            public void ExecuteNext(int firstIndex, int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];

                var move = Vector3.left;
                if (index % 2 == 1)
                {
                    move = Vector3.right;
                }

                float3 drift = math.mul(agent.rotation, DeltaTime) * agent.currentMoveSpeed * (Vector3.forward + move);
                if (agent.nextWaypointIndex != agent.totalWaypoints)
                {
                    var offsetWaypoint = agent.currentWaypoint + drift;
                    var waypointInfo = NavMeshQuery.MapLocation(offsetWaypoint, Vector3.one * 3f, 0, agent.areaMask);
                    if (NavMeshQuery.IsValid(waypointInfo))
                    {
                        agent.currentWaypoint = waypointInfo.position;
                    }
                }
                agent.currentMoveSpeed = Mathf.Max(agent.currentMoveSpeed / 2f, 0.5f);
                var positionInfo = NavMeshQuery.MapLocation(agent.position + drift, Vector3.one * 3f, 0, agent.areaMask);
                if (NavMeshQuery.IsValid(positionInfo))
                {
                    agent.nextPosition = positionInfo.position;
                }
                else
                {
                    agent.nextPosition = agent.position;
                }
                Agents[entity] = agent;
            }
        }

        [BurstCompile]
        struct HashPositionsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;
            [ReadOnly]
            public ComponentDataFromEntity<NavAgent> Agents;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavAgentAvoidance> Avoidances;
            public NativeMultiHashMap<int, int>.ParallelWriter IndexMap;
            public NativeMultiHashMap<int, float3>.ParallelWriter NextPositionMap;
            public int MapSize;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];
                var avoidance = Avoidances[entity];
                var hash = Hash(agent.position, avoidance.radius);
                IndexMap.Add(hash, index);
                NextPositionMap.Add(hash, agent.nextPosition);
                avoidance.partition = hash;
                Avoidances[entity] = avoidance;
            }

            public int Hash(float3 position, float radius)
            {
                int ix = Mathf.RoundToInt((position.x / radius) * radius);
                int iz = Mathf.RoundToInt((position.z / radius) * radius);
                return ix * MapSize + iz;
            }
        }

        private EntityQuery _agentQuery;

        NavMeshQuerySystem _querySystem;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // In theory, JobComponentSystem will by default not run OnUpdate if the agentQuery is empty to begin with.
            var agentCnt = _agentQuery.CalculateEntityCount();

            if (agentCnt > 0)
            {
                var agentEntities = _agentQuery.ToEntityArray(Allocator.TempJob);

                IndexMap.Clear();
                NextPositionMap.Clear();

                var hashPositionsJob = new HashPositionsJob
                {
                    MapSize = _querySystem.MaxMapWidth,
                    Entities = agentEntities,
                    Agents = GetComponentDataFromEntity<NavAgent>(true),
                    Avoidances = GetComponentDataFromEntity<NavAgentAvoidance>(),
                    IndexMap = IndexMap.AsParallelWriter(),
                    NextPositionMap = NextPositionMap.AsParallelWriter()
                };
                var dt = Time.DeltaTime;
                var hashPositionsJobHandle = hashPositionsJob.Schedule(agentCnt, 64, inputDeps);
                var avoidanceJob = new NavAgentAvoidanceJob
                {
                    DeltaTime = dt,
                    indexMap = IndexMap,
                    nextPositionMap = NextPositionMap,
                    Agents = GetComponentDataFromEntity<NavAgent>(),
                    Entities = agentEntities, // Set to deallocate
                    NavMeshQuery = _navMeshQuery
                };
                var avoidanceJobHandle = avoidanceJob.Schedule(IndexMap, 64, hashPositionsJobHandle);


                return avoidanceJobHandle;
            }
            return inputDeps;
        }

        protected override void OnCreate()
        {
            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(NavAgent), typeof(NavAgentAvoidance) }
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);
            _navMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 128);
            IndexMap = new NativeMultiHashMap<int, int>(100 * 1024, Allocator.Persistent);
            NextPositionMap = new NativeMultiHashMap<int, float3>(100 * 1024, Allocator.Persistent);
            _querySystem = World.GetOrCreateSystem<NavMeshQuerySystem>();
        }

        protected override void OnDestroy()
        {

            if (IndexMap.IsCreated) IndexMap.Dispose();
            if (NextPositionMap.IsCreated) NextPositionMap.Dispose();
            _navMeshQuery.Dispose();
        }
    }

}