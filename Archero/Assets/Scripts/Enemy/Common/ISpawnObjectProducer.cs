using System;
using UnityEngine;

namespace Enemy.Common
{
    public interface ISpawnObjectProducer : IDisposable
    {
        void Produce(GameObject prefab,SpawnZone zone);
    }
}