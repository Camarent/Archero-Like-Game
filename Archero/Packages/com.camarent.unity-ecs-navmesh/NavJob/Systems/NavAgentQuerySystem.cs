using System.Collections.Concurrent;
using System.Linq;
using NavJob.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace NavJob.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class NavAgentQuerySystem : ComponentSystem
    {
        public enum PathStatus
        {
            Queued,
            Success,
            Failed,
            Completed
        }

        private struct AgentData
        {
            public Entity entity;
            public NavAgent agent;
            public PathQueryResult result;
        }


        private NavMeshQuerySystem querySystem;
        private readonly ConcurrentDictionary<int, Vector3[]> waypoints = new ConcurrentDictionary<int, Vector3[]>();

        private NativeHashMap<int, AgentData> pathFindingData;
        private NativeQueue<AgentData> setPathResultCommands;

        protected override void OnCreate()
        {
            base.OnCreate();
            querySystem = World.GetOrCreateSystem<NavMeshQuerySystem>();

            querySystem.RegisterPathResolvedCallback(OnPathSuccess);
            querySystem.RegisterPathFailedCallback(OnPathError);

            pathFindingData = new NativeHashMap<int, AgentData>(0, Allocator.Persistent);
            setPathResultCommands = new NativeQueue<AgentData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Entities
                .WithAll<NavAgent>()
                .WithNone<PathQueryResult>()
                .ForEach(entity => PostUpdateCommands.AddComponent<PathQueryResult>(entity));

            Entities.ForEach((Entity entity, ref NavAgent agent, ref PathQueryResult pathQueryResult) =>
            {
                if (agent.status != AgentStatus.PathQueued || pathQueryResult.status != PathStatus.Success || !waypoints.ContainsKey(entity.Index))
                    return;

                var waypointsArray = waypoints[entity.Index];
                if (pathQueryResult.waypointIndex < waypointsArray.Length - 1)
                {
                    ++pathQueryResult.waypointIndex;
                    agent.status = AgentStatus.Moving;
                    pathQueryResult.nextWaypoint = waypointsArray[pathQueryResult.waypointIndex];
                }
                else
                    pathQueryResult.status = PathStatus.Completed;
            });

            Entities
                .ForEach((Entity entity, ref NavAgent agent, ref Translation translation, ref NavAgentTarget target) =>
                {
                    if (!target.needsToProcess) return;
                    SetDestination(entity, agent, translation, target.TargetPosition);
                    PostUpdateCommands.SetComponent(entity, new NavAgentTarget {TargetPosition = target.TargetPosition, lookToTarget = target.lookToTarget, needsToProcess = false});
                    PostUpdateCommands.SetComponent(entity, new PathQueryResult {status = PathStatus.Queued});
                });

            while (setPathResultCommands.TryDequeue(out AgentData agentData))
            {
                PostUpdateCommands.SetComponent(agentData.entity, agentData.agent);
                PostUpdateCommands.SetComponent(agentData.entity, agentData.result);
            }
        }

        public void SetDestination(Entity entity, NavAgent agent, Translation translation, Vector3 destination, int areas = -1)
        {
            if (!pathFindingData.TryAdd(entity.Index, new AgentData {entity = entity, agent = agent}))
                return;

            querySystem.RequestPath(entity.Index, translation.Value, destination, areas);
        }

        private void OnPathSuccess(int index, Vector3[] waypoints)
        {
            if (!pathFindingData.TryGetValue(index, out AgentData entry))
                return;

            this.waypoints.AddOrUpdate(index, waypoints, (i, vector3s) => waypoints);

            entry.agent.status = AgentStatus.Moving;
            entry.agent.queryVersion = querySystem.Version;
            entry.result = new PathQueryResult {status = PathStatus.Success, nextWaypoint = waypoints[0], waypointIndex = 0};

            setPathResultCommands.AsParallelWriter().Enqueue(entry);
            pathFindingData.Remove(index);

            Debug.Log(waypoints.ToList().SerializedView());
        }

        private void OnPathError(int index, PathfindingFailedReason reason)
        {
            if (!pathFindingData.TryGetValue(index, out AgentData entry)) return;

            entry.agent.status = AgentStatus.Idle;
            entry.agent.queryVersion = querySystem.Version;
            entry.result = new PathQueryResult {status = PathStatus.Failed};
            setPathResultCommands.AsParallelWriter().Enqueue(entry);
            pathFindingData.Remove(index);
        }

        protected override void OnDestroy()
        {
            pathFindingData.Dispose();
            setPathResultCommands.Dispose();
            base.OnDestroy();
        }
    }
}