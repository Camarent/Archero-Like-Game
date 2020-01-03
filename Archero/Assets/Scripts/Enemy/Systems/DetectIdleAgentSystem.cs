using NavJob.Components;
using NavJob.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Enemy.Systems
{
    [UpdateBefore(typeof(NavAgentSystem))]
    public class DetectIdleAgentSystem : ComponentSystem
    {
        public struct AgentData
        {
            public int index;
            public Entity entity;
            public NavAgent agent;
        }

        private float _nextUpdate;

        private NativeQueue<AgentData> _needsPath = new NativeQueue<AgentData>(Allocator.Persistent);

        private EntityQuery _agentQuery;
        private NavMeshQuerySystem _navQuery;

        private Transform _playerTransform;
        private Vector3 _prevPlayerPosition;

        [BurstCompile]
        private struct DetectIdleAgentJob : IJobParallelFor
        {
            [ReadOnly] public bool _positionChanged;
            [ReadOnly] public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<NavAgent> Agents;
            public NativeQueue<AgentData>.ParallelWriter NeedsPath;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];

                if (agent.status != AgentStatus.Idle && !_positionChanged)
                    return;

                NeedsPath.Enqueue(new AgentData {index = index, agent = agent, entity = entity});
                agent.status = AgentStatus.PathQueued;
                Agents[entity] = agent;
            }
        }

        private struct SetNextPathJob : IJob
        {
            public Vector3 playerPosition;
            public NativeQueue<AgentData> NeedsPath;

            public void Execute()
            {
                while (NeedsPath.TryDequeue(out AgentData item))
                {
                    NavAgentSystem.SetDestinationStatic(item.entity, item.agent, playerPosition, item.agent.areaMask);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (Time.ElapsedTime > _nextUpdate)
            {
                _nextUpdate = (float) Time.ElapsedTime + 0.5f;
            }

            var entityCnt = _agentQuery.CalculateEntityCount();
            var entities = _agentQuery.ToEntityArray(Allocator.TempJob);

            var targetPosition = Settings.IsPlayerDead() ? _prevPlayerPosition : Settings.PlayerPosition;
            var inputDeps = new DetectIdleAgentJob
            {
                _positionChanged = Vector3.Distance(targetPosition, _prevPlayerPosition) > 0.05f,
                Entities = entities,
                Agents = GetComponentDataFromEntity<NavAgent>(),
                NeedsPath = _needsPath.AsParallelWriter()
            }.Schedule(entityCnt, 64);
            inputDeps = new SetNextPathJob
            {
                playerPosition = targetPosition,
                NeedsPath = _needsPath
            }.Schedule(inputDeps);

            inputDeps.Complete();
            entities.Dispose();
            _prevPlayerPosition = targetPosition;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(NavAgent)}
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);
            _navQuery = World.GetOrCreateSystem<NavMeshQuerySystem>();
        }

        protected override void OnDestroy()
        {
            _needsPath.Dispose();
        }
    }
}