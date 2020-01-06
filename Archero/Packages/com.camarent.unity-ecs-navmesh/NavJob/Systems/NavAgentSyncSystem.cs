using NavJob.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace NavJob.Systems
{
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
    public class NavAgentToVelocitySyncSystem : ComponentSystem
    {
        BuildPhysicsWorld CreatePhysicsWorldSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            CreatePhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }

        protected override void OnUpdate()
        {
            var world = CreatePhysicsWorldSystem.PhysicsWorld;
            Entities.WithAll<PhysicsMass>().ForEach((Entity entity, ref NavAgent agent, ref NavAgentTarget target, ref Translation translation) =>
            {
                if (agent.status != AgentStatus.Moving) return;

                var index = world.GetRigidBodyIndex(entity);

                var linearVelocity = agent.targetLinearVelocity;
                linearVelocity.y = 0;
                world.ApplyLinearImpulse(index, linearVelocity);
                world.ApplyAngularImpulse(index, agent.targetAngularVelocity);
            });
        }
    }

}