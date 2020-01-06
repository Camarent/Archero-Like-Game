#region

using NavJob.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

#endregion

namespace NavJob.Systems
{
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class NavAgentMoveSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct MovementJob : IJobForEach<NavAgent, Translation, Rotation, NavAgentTarget, PathQueryResult>
        {
            public float deltaTime;

            public void Execute(ref NavAgent agent, ref Translation translation, ref Rotation rotation, ref NavAgentTarget target, ref PathQueryResult pathQuery)
            {
                if (agent.status != AgentStatus.Moving || pathQuery.status == NavAgentQuerySystem.PathStatus.Failed)
                {
                    return;
                }

                //Move
                var heading = pathQuery.nextWaypoint - translation.Value;
                heading.y = 0;
                agent.remainingDistance = math.length(heading);
                var isOnPoint = agent.remainingDistance < agent.stoppingDistance;
                if (isOnPoint)
                {
                    agent.status = pathQuery.status == NavAgentQuerySystem.PathStatus.Success ? AgentStatus.PathQueued : AgentStatus.Idle;
                }
                else
                {
                    heading = math.normalize(heading);
                    agent.currentMoveSpeed = math.lerp(
                        agent.currentMoveSpeed,
                        agent.remainingDistance > agent.stoppingDistance ? agent.moveSpeed : 0.75f * agent.moveSpeed,
                        deltaTime * agent.acceleration);
                    agent.targetLinearVelocity = heading * (agent.currentMoveSpeed * deltaTime);
                }

                //Rotation
                if (isOnPoint && !target.lookToTarget) return;

                var forward = target.lookToTarget ? target.TargetPosition - translation.Value : heading;
                forward.y = 0;
                var targetRotation = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));
                // Rotations stack right to left,
                // so first we undo our rotation, then apply the target.
                var delta = math.mul(math.inverse(rotation.Value), targetRotation);

                delta.ToAngleAxis(out var angle, out var axis);

                // We get an infinite axis in the event that our rotation is already aligned.
                if (float.IsInfinity(axis.x))
                    return;

                if (angle > 180)
                    angle -= 360;
                
                // Here I drop down to 0.9f times the desired movement,
                // since we'd rather undershoot and ease into the correct angle
                // than overshoot and oscillate around it in the event of errors.
                var angular = (0.9f * math.radians(angle)) * math.normalize(axis);
                /*Debug.Log($"Forward rotation: {forward}");
                Debug.Log($"Target rotation quaternion: {targetRotation}");
                Debug.Log($"Target rotation euler: {math.degrees(targetRotation.Euler())}");
                Debug.Log($"Delta rotation quaternion: {delta}");
                Debug.Log($"Delta rotation euler: {math.degrees(delta.Euler())}");
                Debug.Log($"Angle: {angle}");
                Debug.Log($"Axis: {math.normalize(axis)}");
                Debug.Log($"Angular velocity: {angular}");*/

                agent.targetAngularVelocity = angular * deltaTime * agent.rotationSpeed;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new MovementJob
            {
                deltaTime = Time.DeltaTime
            }.Schedule(this, inputDeps);
            return inputDeps;
        }
    }
}