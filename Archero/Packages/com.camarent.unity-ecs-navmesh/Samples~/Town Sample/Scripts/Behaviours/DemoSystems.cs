#region

using UnityEngine;
using UnityEngine.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Demo.Behaviours;
using NavJob.Components;
using NavJob.Systems;
using Unity.Collections.LowLevel.Unsafe;

#endregion

namespace Demo
{
    public class SpawnSystem : ComponentSystem
    {
        public int PendingSpawn;

        private PopulationSpawner _spawner;
        private int _lastSpawned;
        private float _nextUpdate;

        private EntityArchetype _agentArch;

        private int _spawned;

        private Text _spawnedText;

        private Text SpawnedText
        {
            get
            {
                if (_spawnedText == null)
                {
                    _spawnedText = GameObject.Find("SpawnedText").GetComponent<Text>();
                }

                return _spawnedText;
            }
        }

        private PopulationSpawner GetSpawner()
        {
            if (_spawner == null)
            {
                _spawner = Object.FindObjectOfType<PopulationSpawner>();
            }

            return _spawner;
        }

        private EntityQuery _spawnQuery;
        protected override void OnCreate()
        {
            base.OnCreate();

            // create the system
            World.CreateSystem<NavAgentSystem>();
            World.GetOrCreateSystem<NavAgentToTransfomMatrixSyncSystem>();
            _buildings = World.GetOrCreateSystem<BuildingCacheSystem>();

            var spawnQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(PendingSpawn) }
            };
            _spawnQuery = GetEntityQuery(spawnQueryDesc);

            _agentArch = EntityManager.CreateArchetype(
                typeof(NavAgent),
                // optional avoidance
                /*typeof(NavAgentAvoidance),
                // optional
                typeof (Translation),
                typeof (Rotation),
                typeof (SyncPositionToNavAgent),
                typeof (SyncRotationToNavAgent),
                typeof (SyncPositionFromNavAgent),
                typeof (SyncRotationFromNavAgent),*/
                typeof(LocalToWorld)
            );


        }

        private BuildingCacheSystem _buildings;

        protected override void OnUpdate()
        {
            if (Time.ElapsedTime > _nextUpdate && _lastSpawned != _spawned)
            {
                _nextUpdate = (float)Time.ElapsedTime + 0.5f;
                _lastSpawned = _spawned;
                SpawnedText.text = $"Spawned: {_spawned} people";
            }

            if (GetSpawner().Renderers.Length == 0)
            {
                return;
            }

            if (_buildings.ResidentialBuildings.Length == 0)
            {
                return;
            }

            var pendings = GetComponentDataFromEntity<PendingSpawn>();
            var entities = _spawnQuery.ToEntityArray(Allocator.TempJob);
            var rootEntity = entities[0];

            var spawnData = pendings[rootEntity];
            PendingSpawn = spawnData.Quantity;
            spawnData.Quantity = 0;
            pendings[rootEntity] = spawnData;
            var manager = EntityManager;
            for (var i = 0; i < PendingSpawn; i++)
            {
                _spawned++;
                var position = _buildings.GetResidentialBuilding();
                var entity = manager.CreateEntity(_agentArch);
                var navAgent = new NavAgent(
                    position,
                    Quaternion.identity,
                    spawnData.AgentStoppingDistance,
                    spawnData.AgentMoveSpeed,
                    spawnData.AgentAcceleration,
                    spawnData.AgentRotationSpeed,
                    spawnData.AgentAreaMask
                );
                // optional if set on the archetype
                // manager.SetComponentData (entity, new Position { Value = position });
                manager.SetComponentData(entity, navAgent);
                // optional for avoidance
                // var navAvoidance = new NavAgentAvoidance(2f);
                // manager.SetComponentData(entity, navAvoidance);
                manager.AddSharedComponentData(entity, GetSpawner().Renderers[UnityEngine.Random.Range(0, GetSpawner().Renderers.Length)].Value);
            }

            entities.Dispose();
        }
    }

    public class DetectIdleAgentSystem : ComponentSystem
    {
        public struct AgentData
        {
            public int index;
            public Entity entity;
            public NavAgent agent;
        }

        private Text _awaitingNavmeshText;

        private Text AwaitingNavmeshText
        {
            get
            {
                if (_awaitingNavmeshText == null)
                {
                    _awaitingNavmeshText = GameObject.Find("AwaitingNavmeshText").GetComponent<Text>();
                }

                return _awaitingNavmeshText;
            }
        }

        private Text _cachedPathText;

        private Text CachedPathText
        {
            get
            {
                if (_cachedPathText == null)
                {
                    _cachedPathText = GameObject.Find("CachedPathText").GetComponent<Text>();
                }

                return _cachedPathText;
            }
        }

        private float _nextUpdate;

        private NativeQueue<AgentData> _needsPath = new NativeQueue<AgentData>(Allocator.Persistent);

        [BurstCompile]
        private struct DetectIdleAgentJob : IJobParallelFor
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavAgent> Agents;
            public NativeQueue<AgentData>.ParallelWriter NeedsPath;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var agent = Agents[entity];
                if (agent.status == AgentStatus.Idle)
                {
                    NeedsPath.Enqueue(new AgentData { index = index, agent = agent, entity = entity });
                    agent.status = AgentStatus.PathQueued;
                    Agents[entity] = agent;
                }
            }
        }

        private struct SetNextPathJob : IJob
        {
            public NativeQueue<AgentData> NeedsPath;
            public void Execute()
            {
                while (NeedsPath.TryDequeue(out AgentData item))
                {
                    var destination = BuildingCacheSystem.GetCommercialBuilding();
                    NavAgentSystem.SetDestinationStatic(item.entity, item.agent, destination, item.agent.areaMask);
                }
            }
        }

        private EntityQuery _agentQuery;
        private NavMeshQuerySystem _navQuery;

        protected override void OnUpdate()
        {
            if (Time.ElapsedTime > _nextUpdate)
            {
                AwaitingNavmeshText.text = $"Awaiting Path: {_navQuery.PendingCount} people";
                CachedPathText.text = $"Cached Paths: {_navQuery.CachedCount}";
                _nextUpdate = (float)Time.ElapsedTime + 0.5f;
            }

            var entityCnt = _agentQuery.CalculateEntityCount();
            var entities = _agentQuery.ToEntityArray(Allocator.TempJob);

            var inputDeps = new DetectIdleAgentJob
            {
                Entities = entities,
                Agents = GetComponentDataFromEntity<NavAgent>(),
                NeedsPath = _needsPath.AsParallelWriter()
            }.Schedule(entityCnt, 64);
            inputDeps = new SetNextPathJob
            {
                NeedsPath = _needsPath
            }.Schedule(inputDeps);
            inputDeps.Complete();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var agentQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(NavAgent) }
            };
            _agentQuery = GetEntityQuery(agentQueryDesc);
            _navQuery = World.GetOrCreateSystem<NavMeshQuerySystem>();
        }

        protected override void OnDestroy()
        {
            _needsPath.Dispose();
        }
    }

    public class BuildingCacheSystem : ComponentSystem
    {
        public NativeList<Vector3> CommercialBuildings = new NativeList<Vector3>(Allocator.Persistent);
        public NativeList<Vector3> ResidentialBuildings = new NativeList<Vector3>(Allocator.Persistent);
        private PopulationSpawner _spawner;
        private int _nextCommercial = 0;
        private int _nextResidential = 0;
        //private EntityQuery buildingQuery;
        private static BuildingCacheSystem _instance;

        protected override void OnCreate()
        {
            _instance = this;
        }

        private PopulationSpawner Spawner
        {
            get
            {
                if (_spawner == null)
                {
                    _spawner = Object.FindObjectOfType<PopulationSpawner>();
                }

                return _spawner;
            }
        }

        public Vector3 GetResidentialBuilding()
        {
            _nextResidential++;
            if (_nextResidential >= ResidentialBuildings.Length)
            {
                _nextResidential = 0;
            }

            return ResidentialBuildings[_nextResidential];
        }

        public static Vector3 GetCommercialBuilding()
        {
            Vector3 building = _instance.CommercialBuildings[0];
            try
            {
                if (_instance._nextCommercial < _instance.CommercialBuildings.Length)
                {
                    building = _instance.CommercialBuildings[_instance._nextCommercial];
                    _instance._nextCommercial++;
                }
                else
                {
                    _instance._nextCommercial = 0;
                }
                return building;
            }
            catch
            {
                return building;
            }
        }

        protected override void OnUpdate()
        {

            Entities.WithAll<BuildingData>().ForEach((ref BuildingData building) =>
            {
                if (building.Type == BuildingType.Residential)
                {
                    ResidentialBuildings.Add(building.Position);
                }
                else
                {
                    CommercialBuildings.Add(building.Position);
                }

                PostUpdateCommands.RemoveComponent<BuildingData>(building.Entity);
            });
        }

        protected override void OnDestroy()
        {
            ResidentialBuildings.Dispose();
            CommercialBuildings.Dispose();
        }
    }
}