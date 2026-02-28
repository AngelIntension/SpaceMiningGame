using Cysharp.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Mining.Views
{
    /// <summary>
    /// Manages ore chunk spawn loop during active mining.
    /// Spawns 2-5 chunks at random intervals (3-7s), using ObjectPool for zero GC.
    /// See FR-013: Chunk spawning, FR-014: Burst spawn pattern.
    /// </summary>
    public sealed class OreChunkController : MonoBehaviour
    {
        private const int PoolSize = 15;

        private IStateStore _stateStore;
        private IEventBus _eventBus;
        private OreChunkConfig _config;

        private ObjectPool<OreChunkBehaviour> _pool;
        private bool _mining;
        private float _spawnTimer;
        private Transform _collectorPoint;

        // ECS lazy-init
        private EntityManager _entityManager;
        private Entity _shipEntity;
        private bool _ecsReady;

        [Inject]
        public void Construct(IStateStore stateStore, IEventBus eventBus, OreChunkConfig config)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
            _config = config;
        }

        private void Start()
        {
            // Create object pool
            _pool = new ObjectPool<OreChunkBehaviour>(
                createFunc: CreateChunk,
                actionOnGet: chunk => { },
                actionOnRelease: chunk => chunk.gameObject.SetActive(false),
                actionOnDestroy: chunk => Destroy(chunk.gameObject),
                collectionCheck: false,
                defaultCapacity: PoolSize,
                maxSize: PoolSize);

            // Find collector point on the ship
            _collectorPoint = transform.FindChildRecursive("CollectorPoint");

            if (_eventBus != null)
            {
                SubscribeStarted().Forget();
                SubscribeStopped().Forget();
            }
        }

        private async UniTaskVoid SubscribeStarted()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<MiningStartedEvent>().WithCancellation(cts))
            {
                _mining = true;
                _spawnTimer = Random.Range(_config.SpawnIntervalMin, _config.SpawnIntervalMax);
            }
        }

        private async UniTaskVoid SubscribeStopped()
        {
            var cts = this.GetCancellationTokenOnDestroy();
            await foreach (var evt in _eventBus.Subscribe<MiningStoppedEvent>().WithCancellation(cts))
            {
                _mining = false;
            }
        }

        private void Update()
        {
            if (!_mining || _config == null) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnChunkBurst();
                _spawnTimer = Random.Range(_config.SpawnIntervalMin, _config.SpawnIntervalMax);
            }
        }

        private void SpawnChunkBurst()
        {
            if (!_ecsReady)
                TryInitializeECS();

            // Get asteroid surface position from ECS
            Vector3 spawnPos = Vector3.zero;
            string oreId = "";
            Color oreColor = Color.white;

            var miningState = _stateStore?.Current.Loop.Mining;
            if (miningState != null && miningState.TargetAsteroidId.HasValue)
            {
                oreId = miningState.ActiveOreId.GetValueOrDefault("");

                if (_ecsReady)
                {
                    int targetIndex = miningState.TargetAsteroidId.GetValueOrDefault(0);
                    var query = _entityManager.CreateEntityQuery(typeof(AsteroidComponent), typeof(LocalTransform));
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i].Index == targetIndex)
                        {
                            var lt = _entityManager.GetComponentData<LocalTransform>(entities[i]);
                            var asteroid = _entityManager.GetComponentData<AsteroidComponent>(entities[i]);
                            Vector3 center = lt.Position;

                            // Compute surface point facing the ship
                            Vector3 toShip = transform.position - center;
                            float dist = toShip.magnitude;
                            spawnPos = dist > 0.001f
                                ? center + (toShip / dist) * asteroid.Radius
                                : center;
                            break;
                        }
                    }
                    entities.Dispose();
                }
            }

            int chunkCount = Random.Range(_config.ChunksPerSpawnMin, _config.ChunksPerSpawnMax + 1);

            for (int i = 0; i < chunkCount; i++)
            {
                if (_pool.CountActive >= PoolSize) break;

                var chunk = _pool.Get();
                float scale = Random.Range(_config.ChunkScaleMin, _config.ChunkScaleMax);
                chunk.transform.localScale = Vector3.one * scale;

                Vector3 driftDir = Random.onUnitSphere;

                chunk.Initialize(
                    spawnPos,
                    driftDir,
                    _collectorPoint,
                    _config,
                    _eventBus,
                    _pool,
                    oreColor,
                    _config.GlowIntensity,
                    oreId);
            }
        }

        private OreChunkBehaviour CreateChunk()
        {
            var go = new GameObject("OreChunk");
            go.transform.SetParent(transform, false);

            // Add simple mesh for visibility
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();

            // Use Unity built-in sphere mesh as placeholder
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            filter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = sphere.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(sphere);

            // Remove collider (cosmetic only)
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var chunk = go.AddComponent<OreChunkBehaviour>();
            go.SetActive(false);
            return chunk;
        }

        private void TryInitializeECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;
            _ecsReady = true;
        }
    }
}
