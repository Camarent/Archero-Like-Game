using Common;
using General.Runtime;
using Unity.Entities;
using Unity.Transforms;

namespace General.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class RemoveDeadSystem : ComponentSystem
    {
        private EntityQuery _enemies;
        protected override void OnCreate()
        {
            base.OnCreate();

            _enemies = GetEntityQuery(typeof(EnemyTag));
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref Health health, ref Translation translation) =>
            {
                if(health.Value > 0f) return;

                if (EntityManager.HasComponent(entity, typeof(PlayerTag)))
                {
                    PlayerSettings.PlayerDied();
                }
                else if (EntityManager.HasComponent(entity, typeof(EnemyTag)))
                {
                    PostUpdateCommands.DestroyEntity(entity);
                    BulletImpactPool.PlayBulletImpact(translation.Value);
                    
                    var instantiated = PostUpdateCommands.Instantiate(PlayerSettings.Coin);
                    PostUpdateCommands.SetComponent(instantiated, new Translation {Value = translation.Value});
                }
                else if (EntityManager.HasComponent<BulletTag>(entity))
                {
                    PostUpdateCommands.DestroyEntity(entity);
                }
            });
            
            var count = _enemies.CalculateEntityCount();
            if(count == 0 && GameManager.Instance.GameStatus == GameManager.Status.Play)
                GameManager.Instance.Success();
        }
    }
}