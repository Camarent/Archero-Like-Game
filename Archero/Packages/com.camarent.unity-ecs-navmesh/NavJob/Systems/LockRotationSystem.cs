using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace NavJob.Systems
{
    [UpdateBefore(typeof(TransformSystemGroup)), UpdateAfter(typeof(ExportPhysicsWorld))]
    public class LockRotationSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return Entities.WithoutBurst().ForEach((Entity entity, ref Rotation rotation, ref LockRotation lockRotation) =>
            {
                var euler = rotation.Value.Euler();
                if (lockRotation.X)
                    euler.x = 0f;
                if (lockRotation.Y)
                    euler.y = 0f;
                if (lockRotation.Z)
                    euler.z = 0f;
                Debug.Log($"Euler before lock: {euler}");
                rotation.Value = quaternion.EulerXYZ(euler);
                Debug.Log($"Applied lock: {rotation.Value.Euler()}");
            }).Schedule(inputDeps);
        }
    }
}