using Common;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Collect.Systems
{
    public class CoinRotationSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(CoinTag))]
        struct RotationJob : IJobForEach<Rotation>
        {
            public float rotationSpeed;
            public float delta;

            public void Execute(ref Rotation rotation)
            {
                rotation.Value = math.mul(quaternion.RotateY(rotationSpeed * delta), rotation.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new RotationJob
            {
                rotationSpeed = PlayerSettings.CoinSpeed,
                delta = Time.DeltaTime
            }.Schedule(this, inputDeps);
        }
    }
}