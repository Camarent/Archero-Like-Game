// This system applies an impulse to any dynamic that collides with a Repulsor.
// A Repulsor is defined by a PhysicsShapeAuthoring with the `Raise Collision Events` flag ticked and a
// CollisionEventImpulse behaviour added.

using General.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace Enemy.Systems
{
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class TickDamagePlayerOnCollisionSystem : JobComponentSystem
    {
        private BuildPhysicsWorld _buildPhysicsWorldSystem;
        private StepPhysicsWorld _stepPhysicsWorldSystem;
        private EntityQuery _playerGroup;

        protected override void OnCreate()
        {
            _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
            _playerGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(PlayerTag),
                    typeof(Health)
                },
            });
        }

        struct TickDamagePlayerOnCollisionJob : ICollisionEventsJob
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> players;

            public ComponentDataFromEntity<Health> healths;
            public ComponentDataFromEntity<TickDamage> TickDamages;

            public void Execute(CollisionEvent collisionEvent)
            {
                var entityA = collisionEvent.Entities.EntityA;
                var entityB = collisionEvent.Entities.EntityB;

                if (!TickDamages.Exists(entityA) || !players.Contains(entityB))
                {
                    var temp = entityA;
                    entityA = entityB;
                    entityB = temp;
                }

                if (!TickDamages.Exists(entityA) || !players.Contains(entityB))
                {
                    return;
                }

                var tick = TickDamages[entityA];
                if (!(tick.CurrentTime > tick.Time)) return;
                var health = healths[entityB];
                health.Value -= tick.Value;
                healths[entityB] = health;

                tick.CurrentTime = 0;
                TickDamages[entityA] = tick;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var jobHandle = new TickDamagePlayerOnCollisionJob
            {
                players = _playerGroup.ToEntityArray(Allocator.TempJob),
                healths = GetComponentDataFromEntity<Health>(),
                TickDamages = GetComponentDataFromEntity<TickDamage>()
            }.Schedule(_stepPhysicsWorldSystem.Simulation, ref _buildPhysicsWorldSystem.PhysicsWorld, inputDeps);

            return jobHandle;
        }
    }
}