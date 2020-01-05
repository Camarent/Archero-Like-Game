using Demo;
using NavJob.Components;
using NavJob.Systems;
using Unity.Entities;
using UnityEngine;

namespace Enemy.Authoring
{
    public class EnemyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float AgentStoppingDistance = 0.1f;
        public float AgentAcceleration = 1;
        public float AgentMoveSpeed = 4;
        public float AgentRotationSpeed = 10;
        public float AgentAvoidanceDiameter = 0.7f;

        [HideInInspector] public int AgentAreaMask = ~(1 << 1);

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var navAgent = new NavAgent(
                AgentStoppingDistance,
                AgentMoveSpeed,
                AgentAcceleration,
                AgentRotationSpeed,
                AgentAreaMask
            );

            dstManager.AddComponentData(entity, navAgent);
        }
    }
}