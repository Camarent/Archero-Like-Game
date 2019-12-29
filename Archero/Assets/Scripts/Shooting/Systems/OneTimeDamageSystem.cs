using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace General.Runtime
{
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class OneTimeDamageSystem : JobComponentSystem
    {
        private StepPhysicsWorld stepPhysicsWorld;
        private BuildPhysicsWorld buildPhysicsWorld;

        private EndSimulationEntityCommandBufferSystem system;

        protected override void OnCreate()
        {
            base.OnCreate();
            stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        private struct ApplyDamagePhysical : ITriggerEventsJob
        {
            [ReadOnly] public ComponentDataFromEntity<Damage> triggerDamageFactorGroup;
            [ReadOnly] public ComponentDataFromEntity<BulletTag> bulletFactorGroup;
            public ComponentDataFromEntity<Health> healthDataGroup;

            public EntityCommandBuffer buffer;

            public void Execute(TriggerEvent triggerEvent)
            {
                var entityA = triggerEvent.Entities.EntityA;
                var entityB = triggerEvent.Entities.EntityB;

                var isBodyATrigger = triggerDamageFactorGroup.Exists(entityA);
                var isBodyBTrigger = triggerDamageFactorGroup.Exists(entityB);

                Debug.Log($"Entity {entityA.Index}");
                Debug.Log($"Entity {entityB.Index}");

                // Ignoring Damages overlapping other Damages
                if (isBodyATrigger && isBodyBTrigger)
                    return;

                if (isBodyATrigger && bulletFactorGroup.Exists(entityA))
                {
                    if (healthDataGroup.Exists(entityB))
                    {
                        var component = healthDataGroup[entityB];
                        component.Value -= 1;
                        healthDataGroup[entityB] = component;
                    }

                    buffer.DestroyEntity(entityA);
                }

                if (isBodyBTrigger && bulletFactorGroup.Exists(entityB))
                {
                    if (healthDataGroup.Exists(entityA))
                    {
                        var component = healthDataGroup[entityA];
                        component.Value -= 1;
                        healthDataGroup[entityA] = component;
                    }

                    buffer.DestroyEntity(entityB);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var processingDamageTriggerHandle = new ApplyDamagePhysical
            {
                buffer = system.CreateCommandBuffer(),
                healthDataGroup = GetComponentDataFromEntity<Health>(),
                triggerDamageFactorGroup = GetComponentDataFromEntity<Damage>(true),
                bulletFactorGroup = GetComponentDataFromEntity<BulletTag>(true)
            }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);

            system.AddJobHandleForProducer(processingDamageTriggerHandle);
            return processingDamageTriggerHandle;
        }
    }
}