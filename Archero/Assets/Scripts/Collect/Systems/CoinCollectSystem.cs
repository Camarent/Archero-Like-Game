using Collect.Runtime;
using Common;
using Unity.Entities;

namespace Collect.Systems
{
    public class CoinCollectSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<CoinTag,CollectedTag>().ForEach((Entity entity) =>
            {
                PostUpdateCommands.DestroyEntity(entity);
                PlayerSettings.IncreaseCoin();
            });
        }
    }
}