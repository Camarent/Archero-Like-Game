using General.Runtime;
using Scripts.ECS.RuntimeComponents;
using Unity.Entities;
using Unity.Transforms;

namespace Scripts.ECS.Systems
{
    public class PlayerTransformUpdateSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            if (Settings.IsPlayerDead()) return;

            Entities.WithAll<PlayerTag>().ForEach((ref Translation translation) =>
                {
                    translation.Value = Settings.PlayerPosition;
                });
        }
    }
}