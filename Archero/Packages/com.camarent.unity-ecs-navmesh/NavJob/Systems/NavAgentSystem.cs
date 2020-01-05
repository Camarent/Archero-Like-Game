#region

using System.Collections.Concurrent;
using System.Linq;
using NavJob.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

#endregion

namespace NavJob.Systems
{
    public struct PathQueryResult : IComponentData
    {
        public NavAgentQuerySystem.PathStatus status;
        public int waypointIndex;
        public float3 nextWaypoint;
    }

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

    public static class MathHelper
    {
        public static float Angle(float3 from, float3 to)
        {
            double num = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            return num < 1.00000000362749E-15 ? 0.0f : (float) math.acos(math.clamp(math.dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            var num1 = Angle(from, to);
            var num2 = (float) (from.y * (double) to.z - from.z * (double) to.y);
            var num3 = (float) (from.z * (double) to.x - from.x * (double) to.z);
            var num4 = (float) (from.x * (double) to.y - from.y * (double) to.x);
            var num5 = Mathf.Sign((float) (axis.x * (double) num2 + axis.y * (double) num3 + axis.z * (double) num4));
            return num1 * num5;
        }

        public static float3 Euler(this quaternion quaternion)
        {
            var q = quaternion.value;
            double3 res;

            var sinr_cosp = +2.0 * (q.w * q.x + q.y * q.z);
            var cosr_cosp = +1.0 - 2.0 * (q.x * q.x + q.y * q.y);
            res.x = math.atan2(sinr_cosp, cosr_cosp);

            var sinp = +2.0 * (q.w * q.y - q.z * q.x);
            if (math.abs(sinp) >= 1)
            {
                res.y = math.PI / 2 * math.sign(sinp);
            }
            else
            {
                res.y = math.asin(sinp);
            }

            var siny_cosp = +2.0 * (q.w * q.z + q.x * q.y);
            var cosy_cosp = +1.0 - 2.0 * (q.y * q.y + q.z * q.z);
            res.z = math.atan2(siny_cosp, cosy_cosp);
            return (float3) res;
        }

        public static void ToAngleAxis(this quaternion quaternion, out float angle, out float3 axis)
        {
            quaternion.ToAngleAxisRad(out angle, out axis);
            angle = math.degrees(angle);
        }

        public static void ToAngleAxisRad(this quaternion q, out float angle, out float3 axis)
        {
            if (math.abs(q.value.w) > 1.0f)
                q = math.normalize(q);
            angle = 2.0f * math.acos(q.value.w); // angle
            var den = math.sqrt(1.0 - q.value.w * q.value.w);
            axis = den > 0.0001f ? q.value.xyz : new float3(1, 0, 0);
        }
    }

    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class NavAgentMoveSystem : JobComponentSystem
    {
        private struct MovementJob : IJobForEach<NavAgent, Translation, Rotation, NavAgentTarget, PathQueryResult>
        {
            public float deltaTime;

            public void Execute(ref NavAgent agent, ref Translation translation, ref Rotation rotation, ref NavAgentTarget target, ref PathQueryResult pathQuery)
            {
                if (agent.status != AgentStatus.Moving || pathQuery.status == NavAgentQuerySystem.PathStatus.Failed)
                {
                    return;
                }

                //Move
                var heading = pathQuery.nextWaypoint - translation.Value;
                heading.y = 0;
                agent.remainingDistance = math.length(heading);
                var isOnPoint = agent.remainingDistance < agent.stoppingDistance;
                if (isOnPoint)
                {
                    Debug.Log("Complete waypoint");
                    agent.status = pathQuery.status == NavAgentQuerySystem.PathStatus.Success ? AgentStatus.PathQueued : AgentStatus.Idle;
                }
                else
                {
                    heading = math.normalize(heading);
                    Debug.Log($"Heading: {heading}");
                    agent.currentMoveSpeed = math.lerp(
                        agent.currentMoveSpeed,
                        agent.remainingDistance > agent.stoppingDistance * 2 ? agent.moveSpeed : 0.25f,
                        deltaTime * agent.acceleration);
                    agent.targetLinearVelocity = heading * (agent.currentMoveSpeed * deltaTime);
                }

                //Rotation
                if (isOnPoint && !target.lookToTarget) return;

                var forward = target.lookToTarget ? target.TargetPosition - translation.Value : heading;
                forward.y = 0;
                var targetRotation = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));
                // Rotations stack right to left,
                // so first we undo our rotation, then apply the target.
                var delta = math.mul(math.inverse(rotation.Value),targetRotation);

                delta.ToAngleAxis(out var angle, out var axis);

                // We get an infinite axis in the event that our rotation is already aligned.
                if (float.IsInfinity(axis.x))
                    return;

                if (angle > 180)
                    angle -= 360;
                // Here I drop down to 0.9f times the desired movement,
                // since we'd rather undershoot and ease into the correct angle
                // than overshoot and oscillate around it in the event of errors.
                var angular = (0.9f * math.radians(angle)) * math.normalize(axis);
                Debug.Log($"Forward rotation: {forward}");
                Debug.Log($"Target rotation quaternion: {targetRotation}");
                Debug.Log($"Target rotation euler: {math.degrees(targetRotation.Euler())}");
                Debug.Log($"Delta rotation quaternion: {delta}");
                Debug.Log($"Delta rotation euler: {math.degrees(delta.Euler())}");
                Debug.Log($"Angle: {angle}");
                Debug.Log($"Axis: {math.normalize(axis)}");
                Debug.Log($"Angular velocity: {angular}");

                agent.targetAngularVelocity = angular * deltaTime * agent.rotationSpeed;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new MovementJob
            {
                deltaTime = Time.DeltaTime
            }.Schedule(this, inputDeps);
            return inputDeps;
        }
    }

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


        private EntityQuery _agentQuery;
        private NavMeshQuerySystem _querySystem;
        private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
        private EntityCommandBuffer _buffer;

        protected override void OnCreate()
        {
            _querySystem = World.GetOrCreateSystem<NavMeshQuerySystem>();
            _endSimulationEntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

            //  _querySystem.RegisterPathResolvedCallback(OnPathSuccess);
            //  _querySystem.RegisterPathFailedCallback(OnPathError);

            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(NavAgent)}
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);

            _needsWaypoint = new NativeQueue<AgentData>(Allocator.Persistent);
            _pathFindingData = new NativeHashMap<int, AgentData>(0, Allocator.Persistent);
        }

        /*
        [BurstCompile]
        private struct DetectNextWaypointJob : IJobParallelFor
        {
            public int navMeshQuerySystemVersion;

            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public ComponentDataFromEntity<NewTarget> Targets;
            public ComponentDataFromEntity<NavAgent> Agents;
            public NativeQueue<AgentData>.ParallelWriter NeedsWaypoint;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];
                if (agent.remainingDistance - agent.stoppingDistance > 0 || agent.status != AgentStatus.Moving ||)
                {
                    return;
                }

                if (agent.nextWaypointIndex != agent.totalWaypoints)
                {
                    NeedsWaypoint.Enqueue(new AgentData {agent = agent, entity = entity, index = index});
                }
                else if (navMeshQuerySystemVersion != agent.queryVersion || agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.totalWaypoints = 0;
                    agent.currentWaypoint = 0;
                    agent.status = AgentStatus.Idle;
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
                while (NeedsWaypoint.TryDequeue(out AgentData item))
                {
                    var entity = item.entity;
                    if (!Instance._waypoints.TryGetValue(entity.Index, out Vector3[] currentWaypoints)) continue;
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

            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<NavAgent> Agents;

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
                    Debug.Log($"Current waypoint: {agent.currentWaypoint}");
                    Debug.Log($"Current position: {agent.position}");
                    var heading = (Vector3) (agent.currentWaypoint - agent.position);
                    heading.y = 0;
                    heading = math.normalize(heading);

                    agent.remainingDistance = heading.magnitude;
                    if (agent.remainingDistance > 0.001f)
                    {
                        var targetRotation = Quaternion.LookRotation(heading, new float3(0, 1, 0)).eulerAngles;
                        targetRotation.x = targetRotation.z = 0;

                        var agentRotation = agent.rotation.eulerAngles;
                        agentRotation.x = agentRotation.z = 0;


                        var angularVelocity = Vector3.Lerp(agentRotation, targetRotation, DeltaTime * agent.rotationSpeed) - agentRotation;

                        Debug.Log($"Agent rotation: {agentRotation}");
                        Debug.Log($"Target rotation: {targetRotation}");
                        Debug.Log($"Result rotation: {angularVelocity}");

                        agent.targetAngularVelocity = angularVelocity;
                        /*agent.rotation = agent.remainingDistance < 1
                            ? Quaternion.Euler(targetRotation)
                            : Quaternion.Slerp(agent.rotation, Quaternion.Euler(targetRotation), DeltaTime * agent.rotationSpeed);#1#
                    }

                    agent.currentMoveSpeed = Mathf.Lerp(agent.currentMoveSpeed, agent.moveSpeed, DeltaTime * agent.acceleration);
                    //var forward = math.forward(agent.rotation) * agent.currentMoveSpeed * DeltaTime;

                    agent.targetLinearVelocity = heading * (agent.currentMoveSpeed * DeltaTime);
                    Debug.Log($"Current speed: {agent.currentMoveSpeed}");
                    Debug.Log($"Heading: {heading}");
                    Debug.Log($"Target velocity: {agent.targetLinearVelocity}");
                    // agent.nextPosition = agent.position + forward;
                    Agents[entity] = agent;
                }
                else if (agent.nextWaypointIndex == agent.totalWaypoints)
                {
                    agent.nextPosition = new float3 {x = Mathf.Infinity, y = Mathf.Infinity, z = Mathf.Infinity};
                    agent.status = AgentStatus.Idle;
                    Agents[entity] = agent;
                }
            }
        }
        */

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            /*var entityCnt = _agentQuery.CalculateEntityCount();
            var entities = _agentQuery.ToEntityArray(Allocator.TempJob);
            _buffer = _endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

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
                Entities = entities,
                Agents = GetComponentDataFromEntity<NavAgent>()
            }.Schedule(entityCnt, 64, inputDeps);*/

            return inputDeps;
        }

        /*/// <summary>
        /// Used to set an agent destination and start the pathfinding process
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agent"></param>
        /// <param name="destination"></param>
        /// <param name="areas"></param>
        public void SetDestination(Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            if (_pathFindingData.TryAdd(entity.Index, new AgentData {index = entity.Index, entity = entity, agent = agent}))
            {
                agent.status = AgentStatus.PathQueued;
                agent.destination = destination;
                agent.queryVersion = _querySystem.Version;
                _querySystem.RequestPath(entity.Index, agent.position, agent.destination, areas);
            }
        }

        private void SetWaypoint(Entity entity, NavAgent agent, Vector3[] newWaypoints)
        {
            _waypoints[entity.Index] = newWaypoints;
            agent.status = AgentStatus.Moving;
            agent.nextWaypointIndex = 1;
            agent.totalWaypoints = newWaypoints.Length;
            agent.currentWaypoint = newWaypoints[0];
            agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
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
                _buffer.SetComponent(entry.entity, entry.agent);
                _pathFindingData.Remove(index);
            }
        }*/

        protected override void OnDestroy()
        {
            _needsWaypoint.Dispose();
            _pathFindingData.Dispose();
        }
    }
}