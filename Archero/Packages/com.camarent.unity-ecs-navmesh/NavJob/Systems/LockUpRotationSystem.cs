using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace NavJob.Systems
{
    [UpdateBefore(typeof(TransformSystemGroup)), UpdateAfter(typeof(ExportPhysicsWorld))]
    public class LockUpRotationSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return Entities.ForEach((Entity entity, ref Translation translation, ref Rotation rotation, ref LockUpRotation lockRotation, ref PhysicsMass mass, ref PhysicsVelocity velocity) =>
            {
                var up = math.mul(rotation.Value, math.up());

                var distance = math.length(math.up() - up);
                if (distance > 0.01f)
                    velocity.ApplyImpulse(mass, translation, rotation, math.up() * distance, new float3(0, 1, 0));
            }).Schedule(inputDeps);
        }
    }
}