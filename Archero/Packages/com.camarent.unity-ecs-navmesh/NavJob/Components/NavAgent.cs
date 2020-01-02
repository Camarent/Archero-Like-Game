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
        public float3 destination;
        public float currentMoveSpeed;
        public int queryVersion;

        public AgentStatus status;
        // public AgentStatus status
        // {
        //     get => _status;
        //     set
        //     {
        //         _status = value;
        //         Debug.Log($"Set status: {value}");
        //     }
        // }

        public float3 position;
        public float3 nextPosition;
        public Quaternion rotation;
        public float remainingDistance;
        public float3 currentWaypoint;
        public int nextWaypointIndex;
        public int totalWaypoints;


        public float3 targetLinearVelocity;
        public float3 targetAngularVelocity;
        public NavAgent(
            float3 position,
            Quaternion rotation,
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
            destination = Vector3.zero;
            currentMoveSpeed = 0;
            queryVersion = 0;
            status = AgentStatus.Idle;
            this.position = position;
            this.rotation = rotation;
            nextPosition = new float3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            remainingDistance = 0;
            currentWaypoint = Vector3.zero;
            nextWaypointIndex = 0;
            totalWaypoints = 0;
            targetAngularVelocity = Vector3.zero;
            targetLinearVelocity = Vector3.zero;
        }
    }
}