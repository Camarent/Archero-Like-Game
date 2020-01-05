using System;
using System.Collections.Generic;

namespace Enemy.Common
{
    public class SpawnProducerFactory : ISpawnProducerFactory
    {
        public enum SpawnType
        {
            GameObject,
            Entity
        }

        private readonly Dictionary<SpawnType, ISpawnObjectProducer> producers;

        public SpawnProducerFactory()
        {
            producers = new Dictionary<SpawnType, ISpawnObjectProducer>
            {
                {SpawnType.GameObject, new GameObjectProducer()},
                {SpawnType.Entity, new EntityProducer()}
            };
        }

        public ISpawnObjectProducer Get(SpawnType type)
        {
            if (producers.ContainsKey(type))
                return producers[type];

            throw new InvalidOperationException($"Did not have this type of producer: {type}");
        }

        public void Dispose()
        {
            foreach (var producer in producers.Values)
            {
                producer.Dispose();
            }

            producers.Clear();
        }
    }
}