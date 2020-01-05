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
    /*/// <summary>
    /// Syncs the transform matrix from the nav agent to a LocalToWorld component
    /// </summary>
    [UpdateAfter(typeof(NavAgentSystem))]
    [DisableAutoCreation]
    public class NavAgentToTransfomMatrixSyncSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Translation), typeof(Rotation))]
        private struct NavAgentToTransfomMatrixSyncSystemJob : IJobForEach<NavAgent, LocalToWorld>
        {
            public void Execute([ReadOnly] ref NavAgent NavAgent, ref LocalToWorld Matrix)
            {
                Matrix.Value = Matrix4x4.TRS(NavAgent.position, NavAgent.rotation, Vector3.one);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new NavAgentToTransfomMatrixSyncSystemJob().Schedule(this, inputDeps);
        }
    }*/

    /// <summary>
    /// Sets the NavAgent position to the Position component
    /// </summary>
    /*[UpdateBefore(typeof(NavAgentSystem))]
    //[DisableAutoCreation]
    public class NavAgentFromPositionSyncSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(SyncPositionToNavAgent))]
        private struct NavAgentFromPositionSyncSystemJob : IJobForEach<NavAgent, Translation>
        {
            public void Execute(ref NavAgent NavAgent, [ReadOnly] ref Translation Position)
            {
                NavAgent.position = Position.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new NavAgentFromPositionSyncSystemJob().Schedule(this, inputDeps);
        }
    }*/
    /*/// <summary>
    /// Sets the Position component to the NavAgent position
    /// </summary>
    [UpdateAfter(typeof(NavAgentSystem))]
    [DisableAutoCreation]
    public class NavAgentToPositionSyncSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(SyncPositionFromNavAgent))]
        private struct NavAgentToPositionSyncSystemJob : IJobForEach<NavAgent, Translation>
        {
            public void Execute([ReadOnly] ref NavAgent NavAgent, ref Translation Position)
            {
                Position.Value = NavAgent.position;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new NavAgentToPositionSyncSystemJob().Schedule(this, inputDeps);
        }
    }*/
    /*/// <summary>
    /// Sets the NavAgent rotation to the Rotation component
    /// </summary>
    [UpdateBefore(typeof(NavAgentSystem))]
    //[DisableAutoCreation]
    public class NavAgentFromRotationSyncSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(SyncRotationToNavAgent))]
        private struct NavAgentFromRotationSyncSystemJob : IJobForEach<NavAgent, Rotation>
        {
            public void Execute(ref NavAgent NavAgent, [ReadOnly] ref Rotation Rotation)
            {
                NavAgent.rotation = Rotation.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new NavAgentFromRotationSyncSystemJob().Schedule(this, inputDeps);
        }
    }*/
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
    public class NavAgentToVelocitySyncSystem : ComponentSystem
    {
        BuildPhysicsWorld CreatePhysicsWorldSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            CreatePhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }

        private struct NavAgentToPhysicsVelocitySyncSystemJob : IJobForEach<NavAgent, Rotation, PhysicsMass, PhysicsVelocity>
        {
            public void Execute([ReadOnly] ref NavAgent agent, ref Rotation rotation, ref PhysicsMass mass, ref PhysicsVelocity velocity)
            {
                if (agent.status != AgentStatus.Moving) return;

                var linearVelocity = agent.targetLinearVelocity;
                linearVelocity.y = 0;
                velocity.ApplyLinearImpulse(mass, linearVelocity);

                Debug.Log($"Target angular: {agent.targetAngularVelocity}");
                velocity.ApplyAngularImpulse(mass, agent.targetAngularVelocity);
                Debug.Log($"Applied angular: {velocity.Angular}");
            }
        }

        protected override void OnUpdate()
        {
            //new NavAgentToPhysicsVelocitySyncSystemJob().Schedule(this);
            var world = CreatePhysicsWorldSystem.PhysicsWorld;
            Entities.WithAll<PhysicsMass>().ForEach((Entity entity, ref NavAgent agent, ref NavAgentTarget target, ref Translation translation) =>
            {
                if (agent.status != AgentStatus.Moving) return;

                var index = world.GetRigidBodyIndex(entity);

                var linearVelocity = agent.targetLinearVelocity;
                linearVelocity.y = 0;
                // world.ApplyLinearImpulse(index, linearVelocity);
                Debug.Log($"Applied angular Target: {agent.targetAngularVelocity}");

                /*var md = world.MotionDatas[index];
                float3 angularImpulseWorldSpace = math.cross(md.WorldFromMotion.pos, agent.targetLinearVelocity);
                float3 angularImpulseMotionSpace = math.rotate(math.inverse(md.WorldFromMotion.rot), angularImpulseWorldSpace);*/
                NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
                MotionVelocity mv = motionVelocities[index];
                mv.AngularVelocity = agent.targetAngularVelocity * 2;
                //mv.ApplyAngularImpulse(agent.targetAngularVelocity);
                motionVelocities[index] = mv;
                //world.ApplyAngularImpulse(index, agent.targetAngularVelocity);
            });
        }
    }

    /*/// <summary>
/// Sets the Rotation component to the NavAgent rotation
/// </summary>
[UpdateAfter(typeof(NavAgentSystem))]
[DisableAutoCreation]
public class NavAgentToRotationSyncSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(SyncRotationFromNavAgent))]
    private struct NavAgentToRotationSyncSystemJob : IJobForEach<NavAgent, Rotation>
    {
        public void Execute([ReadOnly] ref NavAgent NavAgent, ref Rotation Rotation)
        {
            Rotation.Value = NavAgent.rotation;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new NavAgentToRotationSyncSystemJob().Schedule(this, inputDeps);
    }
}*/
}