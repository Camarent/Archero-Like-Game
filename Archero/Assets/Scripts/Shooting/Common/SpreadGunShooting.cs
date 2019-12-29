using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace General.Common
{
    public class SpreadGunShooting : GunShooting
    {
        [Header("Spread Settings")]
        [SerializeField] private int width = 10;

        [SerializeField] private int height = 10;

        [SerializeField] private int angle = 3;

        public override void Shoot()
        {
            var amount = height * width;
            var maxWidth = width > 2 ? width / 2 : width;
            var minWidth = -maxWidth;
            var maxHeight = height > 2 ? height / 2 : height;
            var minHeight = -maxHeight;

            var preparedBullets = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(bulletEntity, preparedBullets);

            var shootPointRotation = shootPoint.rotation.eulerAngles;
            var tempRot = shootPointRotation;
            var index = 0;
            for (var x = minWidth; x < maxWidth; x++)
            {
                tempRot.x = (shootPointRotation.x + angle * x) % 360;
                for (var y = minHeight; y < maxHeight / 2; y++)
                {
                    var bullet = preparedBullets[index];
                    manager.SetComponentData(bullet, new Translation {Value = shootPoint.position});

                    tempRot.y = (shootPointRotation.y + angle * y) % 360;
                    manager.SetComponentData(bullet, new Rotation {Value = Quaternion.Euler(tempRot)});

                    index++;
                }
            }

            preparedBullets.Dispose();
        }
    }
}