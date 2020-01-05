using Common;
using General.Runtime;
using NavJob.Components;
using NavJob.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Enemy.Systems
{
    [UpdateBefore(typeof(NavAgentSystem))]
    public class DetectPlayerSystem : JobComponentSystem
    {
        private float _nextUpdate;

        private EntityQuery _agentQuery;

        private Transform _playerTransform;
        private Vector3 _prevPlayerPosition;

        private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(NavAgent),
                    typeof(EnemyTag)
                }
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);

            _endSimulationEntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        [BurstCompile]
        private struct DetectPlayerPositionJob : IJobParallelFor
        {
            public float3 targetPosition;
            public EntityCommandBuffer.Concurrent buffer;

            [ReadOnly] public NativeArray<Entity> entities;
            [ReadOnly] public ComponentDataFromEntity<NavAgentTarget> targets;

            public void Execute(int index)
            {
                var entity = entities[index];

                if (targets.HasComponent(entity))
                    buffer.SetComponent(index, entity, new NavAgentTarget {TargetPosition = targetPosition, lookToTarget = true, needsToProcess = true});
                else
                    buffer.AddComponent(index, entity, new NavAgentTarget {TargetPosition = targetPosition, lookToTarget = true, needsToProcess = true});
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (GameManager.Instance.GameStatus != GameManager.Status.Play) return inputDeps;

            if (Time.ElapsedTime > _nextUpdate)
            {
                _nextUpdate = (float) Time.ElapsedTime + 0.5f;
            }

            var targetPosition = PlayerSettings.IsPlayerDead() ? _prevPlayerPosition : PlayerSettings.PlayerPosition;
            if (Vector3.Distance(targetPosition, _prevPlayerPosition) < 0.2f)
                return inputDeps;

            var entityCnt = _agentQuery.CalculateEntityCount();
            var entities = _agentQuery.ToEntityArray(Allocator.TempJob);

            var jobHandle = new DetectPlayerPositionJob
            {
                buffer = _endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                targetPosition = targetPosition,
                entities = entities,
                targets = GetComponentDataFromEntity<NavAgentTarget>()
            }.Schedule(entityCnt, 64, inputDeps);
            
            jobHandle.Complete();
            entities.Dispose();
            
            _prevPlayerPosition = targetPosition;

            return jobHandle;
        }
    }
}