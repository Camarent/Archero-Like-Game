using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace Enemy.Systems
{
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class TickEnemyDamageSystem : JobComponentSystem
    {
        [BurstCompile]
        struct ChangeTimeJob : IJobForEach<TickDamage>
        {
            public float deltaTime;

            public void Execute(ref TickDamage tickDamage)
            {
                tickDamage.CurrentTime += deltaTime;
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ChangeTimeJob
            {
                deltaTime = Time.DeltaTime
            }.Schedule(this, inputDeps);
        }
    }
}