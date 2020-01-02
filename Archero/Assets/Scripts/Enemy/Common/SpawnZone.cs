using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnZone : MonoBehaviour
{
    public GameObject prefab;

    public int minAmount;
    public int maxAmount;

    public float offsetY = 1;

    private EntityManager manager;
    private BlobAssetStore store;
    private GameObjectConversionSettings settings;
    private Entity entity;

    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        store = new BlobAssetStore();
        settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, store);
        entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);

        Spawn();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
            Spawn();
    }

    private void OnDestroy()
    {
        store.Dispose();
    }

    void Spawn()
    {
        var amount = Random.Range(minAmount, maxAmount);

        var entities = new NativeArray<Entity>(amount, Allocator.Temp);
        manager.Instantiate(entity, entities);

        for (var i = 0; i < amount; i++)
        {
            var spawned = entities[i];
            manager.SetComponentData(spawned, new Translation {Value = GetPositionInsideZone()});
            manager.SetComponentData(spawned, new Rotation {Value = GetRandomRotation()});
        }
    }

    private Quaternion GetRandomRotation()
    {
        var y = Random.Range(0f, 360f);
        return Quaternion.Euler(0, y, 0);
    }

    private Vector3 GetPositionInsideZone()
    {
        var zoneTransform = transform;
        var localScale = zoneTransform.localScale;
        var x = Random.Range(-localScale.x / 2, localScale.x / 2);
        var z = Random.Range(-localScale.z / 2, localScale.z / 2);

        var position = zoneTransform.position;
        var flooredZonePosition = new Vector3(position.x, -localScale.y / 2 + offsetY, position.z);
        return flooredZonePosition + new Vector3(x, 0, z);
    }
}