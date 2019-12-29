using General.Runtime;
using Scripts.ECS.RuntimeComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Movement.Systems
{
    public class TurnTowardsPlayerSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(EnemyTag))]
        public struct TurnToPlayer : IJobForEach<Translation, Rotation>
        {
            public float3 PlayerPosition;
            public void Execute([ReadOnly] ref Translation position, ref Rotation rotation)
            {
                var heading = PlayerPosition - position.Value;
                heading.y = 0;
                rotation.Value = quaternion.LookRotation(heading, math.up());
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (Settings.IsPlayerDead())
                return inputDeps;

            var job = new TurnToPlayer {PlayerPosition = Settings.PlayerPosition};
            return job.Schedule(this, inputDeps);
        }
    }
}