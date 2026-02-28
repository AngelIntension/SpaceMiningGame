using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Data;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Managed SystemBase that drains mining NativeQueues and dispatches to StateStore.
    /// Bridges ECS (Burst) &lt;-&gt; managed (StateStore/EventBus).
    /// Accumulates fractional yield across frames before adding to inventory.
    /// // CONSTITUTION DEVIATION: DOTS SystemBase cannot use constructor injection;
    /// // static SetDependencies used instead. See MVP-05: Mining beam and yield.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MiningActionDispatchSystem : SystemBase
    {
        private static IStateStore _stateStore;
        private static IEventBus _eventBus;

        // Accumulates fractional yield per ore type across frames
        private readonly Dictionary<int, float> _yieldAccumulators = new();

        public static void SetDependencies(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        protected override void OnCreate()
        {
            RequireForUpdate<OreTypeDatabaseComponent>();
            RequireForUpdate<MiningActionBufferSingleton>();
        }

        protected override void OnUpdate()
        {
            if (_stateStore == null || _eventBus == null) return;

            var miningHandle = World.GetExistingSystem<MiningBeamSystem>();
            if (miningHandle == SystemHandle.Null) return;

            ref var miningSystem = ref World.Unmanaged.GetUnsafeSystemRef<MiningBeamSystem>(miningHandle);

            var oreDb = SystemAPI.GetSingleton<OreTypeDatabaseComponent>();
            ref var oreTypes = ref oreDb.Database.Value.OreTypes;

            // --- Drain yield queue ---
            while (miningSystem.YieldQueue.TryDequeue(out var yieldAction))
            {
                string oreId = OreTypeBlobBakingSystem.GetOreId(yieldAction.OreTypeId);
                if (string.IsNullOrEmpty(oreId)) continue;

                float volumePerUnit = (yieldAction.OreTypeId >= 0 && yieldAction.OreTypeId < oreTypes.Length)
                    ? oreTypes[yieldAction.OreTypeId].VolumePerUnit
                    : 0.1f;

                // Dispatch MiningTickAction to update MiningSessionState
                _stateStore.Dispatch(new MiningTickAction(
                    SystemAPI.Time.DeltaTime,
                    1f, 1f, 0f,
                    yieldAction.Amount
                ));

                // Accumulate fractional yield across frames
                if (!_yieldAccumulators.ContainsKey(yieldAction.OreTypeId))
                    _yieldAccumulators[yieldAction.OreTypeId] = 0f;

                _yieldAccumulators[yieldAction.OreTypeId] += yieldAction.Amount;

                // Extract whole units when accumulator >= 1
                float accumulated = _yieldAccumulators[yieldAction.OreTypeId];
                int wholeUnits = (int)System.Math.Floor(accumulated);

                if (wholeUnits > 0)
                {
                    _yieldAccumulators[yieldAction.OreTypeId] = accumulated - wholeUnits;

                    var prevState = _stateStore.Current;
                    _stateStore.Dispatch(new AddResourceAction(oreId, wholeUnits, volumePerUnit));
                    var newState = _stateStore.Current;

                    if (ReferenceEquals(prevState.Loop.Inventory, newState.Loop.Inventory))
                    {
                        // Deactivate ECS mining beam so depletion/scale/emission systems stop
                        foreach (var beam in SystemAPI.Query<RefRW<MiningBeamComponent>>())
                        {
                            if (beam.ValueRO.Active)
                            {
                                beam.ValueRW.Active = false;
                                break;
                            }
                        }

                        _stateStore.Dispatch(new StopMiningAction());
                        var stoppedEvt = new MiningStoppedEvent(yieldAction.SourceAsteroid.Index, StopReason.CargoFull);
                        _eventBus.Publish(in stoppedEvt);
                        _yieldAccumulators.Clear();
                    }
                    else
                    {
                        var yieldEvt = new MiningYieldEvent(oreId, wholeUnits);
                        _eventBus.Publish(in yieldEvt);

                    }
                }
            }

            // --- Drain depleted queue ---
            while (miningSystem.DepletedQueue.TryDequeue(out var depletedAction))
            {
                _stateStore.Dispatch(new StopMiningAction());
                var evt = new MiningStoppedEvent(depletedAction.Asteroid.Index, StopReason.AsteroidDepleted);
                _eventBus.Publish(in evt);
                _yieldAccumulators.Clear();
            }

            // --- Drain stop queue ---
            while (miningSystem.StopQueue.TryDequeue(out var stopAction))
            {
                _stateStore.Dispatch(new StopMiningAction());
                var evt = new MiningStoppedEvent(stopAction.SourceAsteroid.Index, (StopReason)stopAction.Reason);
                _eventBus.Publish(in evt);
                _yieldAccumulators.Clear();
            }

            // --- Drain threshold crossing queue ---
            var scaleHandle = World.GetExistingSystem<AsteroidScaleSystem>();
            if (scaleHandle != SystemHandle.Null)
            {
                ref var scaleSystem = ref World.Unmanaged.GetUnsafeSystemRef<AsteroidScaleSystem>(scaleHandle);
                while (scaleSystem.ThresholdQueue.TryDequeue(out var thresholdAction))
                {
                    var thresholdEvt = new ThresholdCrossedEvent(
                        thresholdAction.Asteroid.Index,
                        thresholdAction.ThresholdIndex,
                        thresholdAction.Position,
                        thresholdAction.Radius);
                    _eventBus.Publish(in thresholdEvt);
                }
            }

            // --- Dispatch depletion fraction update each frame during active mining ---
            var miningState = _stateStore.Current.Loop.Mining;
            if (miningState.TargetAsteroidId.HasValue)
            {
                int asteroidIndex = miningState.TargetAsteroidId.GetValueOrDefault(0);
                float depletionFraction = 0f;

                // Find the target asteroid entity and read its depletion
                foreach (var (asteroid, entity) in
                    SystemAPI.Query<RefRO<AsteroidComponent>>().WithEntityAccess())
                {
                    if (entity.Index == asteroidIndex)
                    {
                        depletionFraction = asteroid.ValueRO.Depletion;
                        break;
                    }
                }

                _stateStore.Dispatch(new MiningDepletionTickAction(depletionFraction));
            }
        }
    }
}
