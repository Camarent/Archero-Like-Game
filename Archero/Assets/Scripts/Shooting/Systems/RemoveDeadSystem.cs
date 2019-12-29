using General.Runtime;
using Unity.Entities;
using Unity.Transforms;

namespace General.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class RemoveDeadSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref Health health, ref Translation translation) =>
            {
                if(health.Value > 0f) return;

                if (EntityManager.HasComponent(entity, typeof(PlayerTag)))
                {
                    Settings.PlayerDied();
                }
                else if (EntityManager.HasComponent(entity, typeof(EnemyTag)))
                {
                    PostUpdateCommands.DestroyEntity(entity);
                    BulletImpactPool.PlayBulletImpact(translation.Value);
                }
                else if (EntityManager.HasComponent<BulletTag>(entity))
                {
                    PostUpdateCommands.DestroyEntity(entity);
                }
            });
        }
    }
}