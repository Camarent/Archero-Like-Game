using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NavJob.Components
{
    public enum AgentStatus
    {
        Idle = 0,
        PathQueued = 1,
        Moving = 2,
        Paused = 4
    }

    [Serializable]
    public struct NavAgent : IComponentData
    {
        public float stoppingDistance;
        public float moveSpeed;
        public float acceleration;
        public float rotationSpeed;
        public int areaMask;
        public float currentMoveSpeed;
        public int queryVersion;

        public AgentStatus status;

        public float remainingDistance;

        public float3 targetLinearVelocity;
        public float3 targetAngularVelocity;
        
        public NavAgent(
            float stoppingDistance = 1f,
            float moveSpeed = 4f,
            float acceleration = 1f,
            float rotationSpeed = 10f,
            int areaMask = -1
        )
        {
            this.stoppingDistance = stoppingDistance;
            this.moveSpeed = moveSpeed;
            this.acceleration = acceleration;
            this.rotationSpeed = rotationSpeed;
            this.areaMask = areaMask;
            
            currentMoveSpeed = 0;
            queryVersion = 0;
            status = AgentStatus.Idle;
            remainingDistance = 0;
            targetAngularVelocity = Vector3.zero;
            targetLinearVelocity = Vector3.zero;
        }
    }
}