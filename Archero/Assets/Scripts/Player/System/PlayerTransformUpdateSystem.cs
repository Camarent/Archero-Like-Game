using Common;
using General.Runtime;
using Unity.Entities;
using Unity.Transforms;

namespace Player.System
{
    public class PlayerTransformUpdateSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            if (PlayerSettings.IsPlayerDead()) return;

            Entities.WithAll<PlayerTag>().ForEach((ref Translation translation, ref Rotation rotation) =>
            {
               PlayerSettings.PlayerTransform.position = translation.Value;
               PlayerSettings.PlayerTransform.rotation = rotation.Value;
            });
        }
    }
}