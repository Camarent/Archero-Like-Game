using Shooting.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace General.Common
{
    public class SpreadGunShooting : GunShooting
    {
        [Header("Spread Settings")] 
        [SerializeField] private int height = 10;

        [SerializeField] private int width = 10;

        [SerializeField] private int angle = 3;

        protected override void SpawnBullet()
        {
            var amount = width * height;
            var maxHeight = height > 2 ? height / 2 : height;
            var minHeight = height > 2 ? -maxHeight : 0;
            var maxWidth = width > 2 ? width / 2 : width;
            var minWidth = width > 2 ? -maxWidth : 0;

            var preparedBullets = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(bulletEntity, preparedBullets);

            var shootPointPosition = shootPoint.position;
            var shootPointRotation = shootPoint.rotation.eulerAngles;

            var bulletDirectionByRotation = Vector3.zero;
            var index = 0;

            for (var x = minHeight; x < maxHeight; x++)
            {
                bulletDirectionByRotation.x = (shootPointRotation.x + angle * x) % 360;
                for (var y = minWidth; y < maxWidth; y++)
                {
                    var bullet = preparedBullets[index];
                    manager.SetComponentData(bullet, new Translation {Value = shootPointPosition});

                    bulletDirectionByRotation.y = (shootPointRotation.y + angle * y) % 360;
                    manager.SetComponentData(bullet, new Rotation {Value = Quaternion.Euler(bulletDirectionByRotation)});

                    index++;
                }
            }

            preparedBullets.Dispose();
        }
    }
}