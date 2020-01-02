using NavJob.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace NavJob.Systems
{
    /// <summary>
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
    }

    /// <summary>
    /// Sets the NavAgent position to the Position component
    /// </summary>
    [UpdateBefore(typeof(NavAgentSystem))]
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
    }

    /// <summary>
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
    }

    /// <summary>
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
    }

    /// <summary>
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
    }

    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class NavAgentToVelocitySyncSystem : JobComponentSystem
    {
        private struct NavAgentToPhysicsVelocitySyncSystemJob : IJobForEach<NavAgent, PhysicsMass, PhysicsVelocity>
        {
            public void Execute([ReadOnly] ref NavAgent agent, ref PhysicsMass mass, ref PhysicsVelocity velocity)
            {
                if (agent.status != AgentStatus.Moving) return;
                velocity.ApplyLinearImpulse(mass, math.normalize(agent.nextPosition - agent.position) * agent.acceleration);
                velocity.ApplyAngularImpulse(mass, agent.targetAngularVelocity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new NavAgentToPhysicsVelocitySyncSystemJob().Schedule(this, inputDeps);
        }
    }
}