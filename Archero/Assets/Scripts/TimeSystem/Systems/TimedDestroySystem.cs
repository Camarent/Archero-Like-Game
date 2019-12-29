using General.Runtime;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace General.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class TimedDestroySystem : JobComponentSystem
    {
        private EndSimulationEntityCommandBufferSystem system;

        protected override void OnCreate()
        {
            base.OnCreate();
            system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        public struct TimeDestroy : IJobForEachWithEntity<TimeToLive>
        {
            public float deltaTime;
            public EntityCommandBuffer.Concurrent buffer;

            public void Execute(Entity entity, int index, ref TimeToLive timeToLive)
            {
                timeToLive.Value -= deltaTime;
                if (timeToLive.Value < 0)
                    buffer.DestroyEntity(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var buffer = system.CreateCommandBuffer().ToConcurrent();
            var job = new TimeDestroy {buffer = buffer, deltaTime = Time.DeltaTime};
            var handle = job.Schedule(this, inputDeps);
            system.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}