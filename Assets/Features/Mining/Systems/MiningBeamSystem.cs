using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Burst-compiled mining beam simulation. Each tick: checks distance, computes yield,
    /// subtracts from asteroid, writes to NativeQueue for managed dispatch.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VoidHarvest.Features.Ship.Systems.ShipPhysicsSystem))]
    public partial struct MiningBeamSystem : ISystem
    {
        private NativeQueue<NativeMiningYieldAction> _yieldQueue;
        private NativeQueue<NativeAsteroidDepletedAction> _depletedQueue;
        private NativeQueue<NativeMiningStopAction> _stopQueue;

        /// <summary>Yield actions produced each tick for managed dispatch. See MVP-05: Mining beam and yield.</summary>
        public NativeQueue<NativeMiningYieldAction> YieldQueue => _yieldQueue;

        /// <summary>Depleted asteroid notifications for managed dispatch. See MVP-05: Mining beam and yield.</summary>
        public NativeQueue<NativeAsteroidDepletedAction> DepletedQueue => _depletedQueue;

        /// <summary>Mining stop notifications (out-of-range, invalid target). See MVP-05: Mining beam and yield.</summary>
        public NativeQueue<NativeMiningStopAction> StopQueue => _stopQueue;

        /// <summary>
        /// Allocate persistent NativeQueues and require player + ore database. See MVP-05: Mining beam and yield.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            _yieldQueue = new NativeQueue<NativeMiningYieldAction>(Allocator.Persistent);
            _depletedQueue = new NativeQueue<NativeAsteroidDepletedAction>(Allocator.Persistent);
            _stopQueue = new NativeQueue<NativeMiningStopAction>(Allocator.Persistent);

            state.RequireForUpdate<PlayerControlledTag>();
            state.RequireForUpdate<OreTypeDatabaseComponent>();
        }

        /// <summary>
        /// Dispose persistent NativeQueues. See MVP-05: Mining beam and yield.
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            if (_yieldQueue.IsCreated) _yieldQueue.Dispose();
            if (_depletedQueue.IsCreated) _depletedQueue.Dispose();
            if (_stopQueue.IsCreated) _stopQueue.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Get ore database
            var oreDbEntity = SystemAPI.GetSingletonEntity<OreTypeDatabaseComponent>();
            var oreDb = SystemAPI.GetComponent<OreTypeDatabaseComponent>(oreDbEntity);
            ref var oreTypes = ref oreDb.Database.Value.OreTypes;

            // Find player ship with mining beam
            foreach (var (beam, shipPos, shipConfig, tag)
                in SystemAPI.Query<
                    RefRW<MiningBeamComponent>,
                    RefRO<ShipPositionComponent>,
                    RefRO<ShipConfigComponent>,
                    RefRO<PlayerControlledTag>>())
            {
                if (!beam.ValueRO.Active) continue;

                var targetEntity = beam.ValueRO.TargetAsteroid;

                // Check if target entity still exists and has components
                if (!SystemAPI.HasComponent<AsteroidComponent>(targetEntity) ||
                    !SystemAPI.HasComponent<AsteroidOreComponent>(targetEntity) ||
                    !SystemAPI.HasComponent<Unity.Transforms.LocalTransform>(targetEntity))
                {
                    beam.ValueRW.Active = false;
                    _stopQueue.Enqueue(new NativeMiningStopAction
                    {
                        SourceAsteroid = targetEntity,
                        Reason = 3 // Invalid target
                    });
                    continue;
                }

                // Check distance
                var asteroidTransform = SystemAPI.GetComponent<Unity.Transforms.LocalTransform>(targetEntity);
                float distance = math.length(asteroidTransform.Position - shipPos.ValueRO.Position);

                if (distance > beam.ValueRO.MaxRange)
                {
                    beam.ValueRW.Active = false;
                    _stopQueue.Enqueue(new NativeMiningStopAction
                    {
                        SourceAsteroid = targetEntity,
                        Reason = 0 // OutOfRange
                    });
                    continue;
                }

                // Read ore data
                var oreComp = SystemAPI.GetComponent<AsteroidOreComponent>(targetEntity);
                var asteroid = SystemAPI.GetComponent<AsteroidComponent>(targetEntity);

                if (oreComp.OreTypeId < 0 || oreComp.OreTypeId >= oreTypes.Length)
                    continue;

                ref var oreData = ref oreTypes[oreComp.OreTypeId];

                // Compute yield
                float hardness = oreData.Hardness;
                float denominator = hardness * (1f + oreComp.Depth);
                float yieldAmount = denominator > 0f
                    ? (beam.ValueRO.MiningPower * oreData.BaseYieldPerSecond * dt) / denominator
                    : 0f;

                // Subtract from asteroid
                float newMass = asteroid.RemainingMass - yieldAmount;
                if (newMass <= 0f)
                {
                    newMass = 0f;
                    yieldAmount = asteroid.RemainingMass; // Don't yield more than remaining
                }

                var newDepletion = asteroid.InitialMass > 0f
                    ? 1f - (newMass / asteroid.InitialMass)
                    : 1f;

                // Write back asteroid state — preserve new fields (PristineTintedColor,
                // CrumbleThresholdsPassed, CrumblePauseTimer, FadeOutTimer) set at spawn
                var updatedAsteroid = asteroid;
                updatedAsteroid.RemainingMass = newMass;
                updatedAsteroid.Depletion = newDepletion;
                SystemAPI.SetComponent(targetEntity, updatedAsteroid);

                // Enqueue yield
                if (yieldAmount > 0f)
                {
                    _yieldQueue.Enqueue(new NativeMiningYieldAction
                    {
                        SourceAsteroid = targetEntity,
                        OreTypeId = oreComp.OreTypeId,
                        Amount = yieldAmount
                    });
                }

                // Check depletion
                if (newMass <= 0f)
                {
                    beam.ValueRW.Active = false;
                    _depletedQueue.Enqueue(new NativeAsteroidDepletedAction
                    {
                        Asteroid = targetEntity
                    });
                }
            }
        }
    }
}
