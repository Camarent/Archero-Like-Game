using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Enemy.Common
{
    public class EntityProducer : ISpawnObjectProducer
    {
        private readonly EntityManager manager;
        private readonly BlobAssetStore store;
        private readonly GameObjectConversionSettings settings;

        private Dictionary<GameObject, Entity> objectsToEntity = new Dictionary<GameObject, Entity>();

        public EntityProducer()
        {
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            store = new BlobAssetStore();
            settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, store);
        }

        public void Produce(GameObject prefab, SpawnZone zone)
        {
            var entity = GetEntity(prefab);

            var amount = Random.Range(zone.MinAmount, zone.MaxAmount);

            var entities = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(entity, entities);

            for (var i = 0; i < amount; i++)
            {
                var spawned = entities[i];
                var attempts = 5;
                while (attempts > 0)
                {
                    var position = zone.GetPositionInsideZone();

                    var isItFree = true;
                    for (var j = 0; j < i - 1; j++)
                    {
                        if (Vector3.Distance(position, manager.GetComponentData<Translation>(entities[j]).Value) < zone.Space)
                            isItFree = false;
                    }

                    if (!isItFree)
                    {
                        ++attempts;
                        continue;
                    }

                    manager.SetComponentData(spawned, new Translation {Value = position});
                    manager.SetComponentData(spawned, new Rotation {Value = zone.GetRandomRotation()});
                    break;
                }
            }
        }

        private Entity GetEntity(GameObject prefab)
        {
            Entity entity;
            if (objectsToEntity.ContainsKey(prefab))
                entity = objectsToEntity[prefab];
            else
            {
                entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
                objectsToEntity.Add(prefab, entity);
            }

            return entity;
        }

        public void Dispose()
        {
            objectsToEntity.Clear();
            store.Dispose();
        }
    }
}