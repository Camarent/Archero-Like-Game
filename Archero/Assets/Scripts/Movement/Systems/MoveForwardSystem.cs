using Movement.Runtime;
using Scripts.ECS.RuntimeComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Movement.Systems
{
    public class MoveForwardSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(MoveForward))]
        public struct Move : IJobForEach<Translation, Rotation, MoveSpeed>
        {
            public float deltaTime;
            public void Execute(ref Translation position, [ReadOnly] ref Rotation rotation,
                [ReadOnly] ref MoveSpeed speed)
            {
                position.Value += deltaTime * speed.Value * math.forward(rotation.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new Move {deltaTime = Time.DeltaTime};
            return job.Schedule(this, inputDeps);
        }
    }
}