using Collect.Runtime;
using General.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Collect.Systems
{
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class CoinPlayerCollisionSystem : JobComponentSystem
    {
        private StepPhysicsWorld stepPhysicsWorld;
        private BuildPhysicsWorld buildPhysicsWorld;

        private EndSimulationEntityCommandBufferSystem system;

        private EntityQuery playerGroup;

        protected override void OnCreate()
        {
            base.OnCreate();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

            playerGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(PlayerTag)
                }
            });
        }

        private struct ApplyDamagePhysical : ITriggerEventsJob
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> players;
            [ReadOnly] public ComponentDataFromEntity<CoinTag> coins;

            public EntityCommandBuffer buffer;

            public void Execute(TriggerEvent triggerEvent)
            {
                var entityA = triggerEvent.Entities.EntityA;
                var entityB = triggerEvent.Entities.EntityB;

                if (!coins.Exists(entityA) || !players.Contains(entityB))
                {
                    var temp = entityA;
                    entityA = entityB;
                    entityB = temp;
                }

                if (!coins.Exists(entityA) || !players.Contains(entityB))
                {
                    return;
                }

                buffer.AddComponent<CollectedTag>(entityA);
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var processingDamageTriggerHandle = new ApplyDamagePhysical
            {
                buffer = system.CreateCommandBuffer(),
                players = playerGroup.ToEntityArray(Allocator.TempJob),
                coins = GetComponentDataFromEntity<CoinTag>(true)
            }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);

            system.AddJobHandleForProducer(processingDamageTriggerHandle);
            return processingDamageTriggerHandle;
        }
    }
}