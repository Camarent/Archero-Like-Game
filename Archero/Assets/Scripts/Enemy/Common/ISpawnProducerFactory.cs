using System;

namespace Enemy.Common
{
    public interface ISpawnProducerFactory : IDisposable
    {
        ISpawnObjectProducer Get(SpawnProducerFactory.SpawnType type);
    }
}