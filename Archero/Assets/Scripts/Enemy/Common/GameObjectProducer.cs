using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Common
{
    public class GameObjectProducer : ISpawnObjectProducer
    {
        public void Produce(GameObject prefab, SpawnZone zone)
        {
            var amount = Random.Range(zone.MinAmount, zone.MaxAmount);

            var spawnedObjects = new List<GameObject>();

            for (var i = 0; i < amount; i++)
            {
                var attempts = 5;
                while (attempts > 0)
                {
                    var position = zone.GetPositionInsideZone();

                    var isItFree = true;
                    for (var j = 0; j < i - 1; j++)
                    {
                        if (Vector3.Distance(position, spawnedObjects[j].transform.position) < zone.Space)
                            isItFree = false;
                    }

                    if (!isItFree)
                    {
                        ++attempts;
                        continue;
                    }

                    var spawned = Object.Instantiate(prefab, position, zone.GetRandomRotation());
                    spawnedObjects.Add(spawned);
                    break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}