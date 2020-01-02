#region

using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using NavJob.Components;

#endregion

namespace NavJob.Systems
{

    class SetDestinationBarrier : EntityCommandBufferSystem { }
    class PathSuccessBarrier : EntityCommandBufferSystem { }
    class PathErrorBarrier : EntityCommandBufferSystem { }
    

    public class NavAgentSystem : JobComponentSystem
    {

        private struct AgentData
        {
            public int index;
            public Entity entity;
            public NavAgent agent;
        }

        private NativeQueue<AgentData> _needsWaypoint;
        private readonly ConcurrentDictionary<int, Vector3[]> _waypoints = new ConcurrentDictionary<int, Vector3[]>();
        private NativeHashMap<int, AgentData> _pathFindingData;


        private struct DetectNextWaypointJob : IJobParallelFor
        {
            public int navMeshQuerySystemVersion;

            [ReadOnly]
            public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavAgent> Agents;
            public NativeQueue<AgentData>.ParallelWriter NeedsWaypoint;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];
                if (agent.remainingDistance - agent.stoppingDistance > 0 || agent.status != AgentStatus.Moving)
                {
                    return;
                }

                if (agent.nextWaypointIndex != agent.totalWaypoints)
                {
                    NeedsWaypoint.Enqueue(new AgentData { agent = agent, entity = entity, index = index });
                }
                else if (navMeshQuerySystemVersion != agent.queryVersion || agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.totalWaypoints = 0;
                    agent.currentWaypoint = 0;
                    agent.status = AgentStatus.Idle;
                    Debug.Log($"Agent {entity.Index} status: {agent.status}");
                    Agents[entity] = agent;
                }
            }
        }

        private struct SetNextWaypointJob : IJob
        {
            public ComponentDataFromEntity<NavAgent> Agents;
            public NativeQueue<AgentData> NeedsWaypoint;
            public void Execute()
            {
                // TODO: Don't like how this one converted...
                while (NeedsWaypoint.TryDequeue(out AgentData item))
                {
                    var entity = item.entity;
                    if (!NavAgentSystem.Instance._waypoints.TryGetValue(entity.Index, out Vector3[] currentWaypoints)) continue;
                    var agent = item.agent;
                    agent.currentWaypoint = currentWaypoints[agent.nextWaypointIndex];
                    agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
                    agent.nextWaypointIndex++;
                    Agents[entity] = agent;
                }
            }
        }


        private struct MovementJob : IJobParallelFor
        {
            public float DeltaTime;
            public float3 Up;
            public float3 One;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavAgent> Agents;

            public void Execute(int index)
            {
                var entity = Entities[index];

                var agent = Agents[entity];
                if (agent.status != AgentStatus.Moving)
                {
                    return;
                }
                if (agent.remainingDistance > 0)
                {
                    agent.currentMoveSpeed = Mathf.Lerp(agent.currentMoveSpeed, agent.moveSpeed, DeltaTime * agent.acceleration);
                    // todo: deceleration
                    if (!float.IsPositiveInfinity(agent.nextPosition.x))
                    {
                        agent.position = agent.nextPosition;
                    }
                    var heading = (Vector3)(agent.currentWaypoint - agent.position);
                    agent.remainingDistance = heading.magnitude;
                    if (agent.remainingDistance > 0.001f)
                    {
                        var targetRotation = Quaternion.LookRotation(heading, Up).eulerAngles;
                        targetRotation.x = targetRotation.z = 0;
                        agent.targetAngularVelocity = targetRotation;
                        agent.rotation = agent.remainingDistance < 1 ? Quaternion.Euler(targetRotation) : Quaternion.Slerp(agent.rotation, Quaternion.Euler(targetRotation), DeltaTime * agent.rotationSpeed);
                    }
                    var forward = math.forward(agent.rotation) * agent.currentMoveSpeed * DeltaTime;
                    agent.nextPosition = agent.position + forward;
                    Agents[entity] = agent;
                }
                else if (agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.nextPosition = new float3 { x = Mathf.Infinity, y = Mathf.Infinity, z = Mathf.Infinity };
                    agent.status = AgentStatus.Idle;
                    Agents[entity] = agent;
                }
            }
        }

        private EntityQuery _agentQuery;
        private NavMeshQuerySystem _querySystem;
        private SetDestinationBarrier _setDestinationBarrier;
        private PathSuccessBarrier _pathSuccessBarrier;
        private PathErrorBarrier _pathErrorBarrier;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entityCnt = _agentQuery.CalculateEntityCount();
            var entities = _agentQuery.ToEntityArray(Allocator.TempJob);

            var dt = Time.DeltaTime;
            inputDeps = new DetectNextWaypointJob
            {
                Entities = entities,
                Agents = GetComponentDataFromEntity<NavAgent>(),
                NeedsWaypoint = _needsWaypoint.AsParallelWriter(),
                navMeshQuerySystemVersion = _querySystem.Version
            }.Schedule(entityCnt, 64, inputDeps);

            inputDeps = new SetNextWaypointJob
            {
                Agents = GetComponentDataFromEntity<NavAgent>(),
                NeedsWaypoint = _needsWaypoint
            }.Schedule(inputDeps);

            inputDeps = new MovementJob
            {
                DeltaTime = dt,
                Up = Vector3.up,
                One = Vector3.one,
                Entities = entities,
                Agents = GetComponentDataFromEntity<NavAgent>()
            }.Schedule(entityCnt, 64, inputDeps);

            return inputDeps;
        }

        /// <summary>
        /// Used to set an agent destination and start the pathfinding process
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agent"></param>
        /// <param name="destination"></param>
        /// <param name="areas"></param>
        public void SetDestination(Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            if (_pathFindingData.TryAdd(entity.Index, new AgentData { index = entity.Index, entity = entity, agent = agent }))
            {
                var command = _setDestinationBarrier.CreateCommandBuffer();
                agent.status = AgentStatus.PathQueued;
                Debug.Log($"Agent {entity.Index} status: {agent.status}");
                agent.destination = destination;
                agent.queryVersion = _querySystem.Version;
                command.SetComponent<NavAgent>(entity, agent);
                _querySystem.RequestPath(entity.Index, agent.position, agent.destination, areas);
            }
        }

        /// <summary>
        /// Static counterpart of SetDestination
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agent"></param>
        /// <param name="destination"></param>
        public static void SetDestinationStatic(Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            Instance.SetDestination(entity, agent, destination, areas);
        }

        protected static NavAgentSystem Instance;

        protected override void OnCreate()
        {
            Instance = this;

            _querySystem = World.GetOrCreateSystem<NavMeshQuerySystem>();
            _setDestinationBarrier = World.GetOrCreateSystem<SetDestinationBarrier>();
            _pathSuccessBarrier = World.GetOrCreateSystem<PathSuccessBarrier>();
            _pathErrorBarrier = World.GetOrCreateSystem<PathErrorBarrier>();

            _querySystem.RegisterPathResolvedCallback(OnPathSuccess);
            _querySystem.RegisterPathFailedCallback(OnPathError);

            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(NavAgent) }
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);

            _needsWaypoint = new NativeQueue<AgentData>(Allocator.Persistent);
            _pathFindingData = new NativeHashMap<int, AgentData>(0, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _needsWaypoint.Dispose();
            _pathFindingData.Dispose();
        }

        private void SetWaypoint(Entity entity, NavAgent agent, Vector3[] newWaypoints)
        {
            _waypoints[entity.Index] = newWaypoints;
            var command = _pathSuccessBarrier.CreateCommandBuffer();
            agent.status = AgentStatus.Moving;
            Debug.Log($"Agent {entity.Index} status: {agent.status}");
            agent.nextWaypointIndex = 1;
            agent.totalWaypoints = newWaypoints.Length;
            agent.currentWaypoint = newWaypoints[0];
            agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
            command.SetComponent<NavAgent>(entity, agent);
        }

        private void OnPathSuccess(int index, Vector3[] waypoints)
        {
            if (_pathFindingData.TryGetValue(index, out AgentData entry))
            {
                SetWaypoint(entry.entity, entry.agent, waypoints);
                _pathFindingData.Remove(index);
            }
        }

        private void OnPathError(int index, PathfindingFailedReason reason)
        {
            if (_pathFindingData.TryGetValue(index, out AgentData entry))
            {
                entry.agent.status = AgentStatus.Idle;
                Debug.Log($"Agent {entry.entity.Index} status: {entry.agent.status}");
                var command = _pathErrorBarrier.CreateCommandBuffer();
                command.SetComponent<NavAgent>(entry.entity, entry.agent);
                _pathFindingData.Remove(index);
            }
        }
    }
}