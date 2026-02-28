using Unity.Entities;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.EventBus.Events;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Docking.Systems
{
    /// <summary>
    /// Managed ISystem: reads DockingEventFlags written by DockingSystem (Burst),
    /// dispatches managed actions and events, then clears the flags.
    /// Preserves zero-GC guarantee in the Burst hot path.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DockingSystem))]
    public partial class DockingEventBridgeSystem : SystemBase
    {
        private static IStateStore _stateStore;
        private static IEventBus _eventBus;

        public static void SetDependencies(IStateStore stateStore, IEventBus eventBus)
        {
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        protected override void OnUpdate()
        {
            if (_stateStore == null || _eventBus == null) return;

            bool shouldRemoveDocking = false;

            foreach (var flags in SystemAPI.Query<RefRW<DockingEventFlags>>())
            {
                if (flags.ValueRO.DockCompleted)
                {
                    int stationId = flags.ValueRO.DockStationId;

                    // Dispatch completion actions to state store
                    _stateStore.Dispatch(new CompleteDockingAction(stationId));
                    _stateStore.Dispatch(new DockAtStationAction(stationId));

                    // Publish event for UI/feedback systems
                    _eventBus.Publish(new DockingCompletedEvent(stationId));

                    // Clear flag
                    flags.ValueRW.DockCompleted = false;
                    flags.ValueRW.DockStationId = 0;
                }

                if (flags.ValueRO.UndockCompleted)
                {
                    // Get station ID from current docking state before clearing
                    int stationId = _stateStore.Current.Loop.Docking.TargetStationId.GetValueOrDefault(-1);

                    // Dispatch completion actions
                    _stateStore.Dispatch(new CompleteUndockingAction());
                    _stateStore.Dispatch(new UndockFromStationAction());

                    // Publish event
                    _eventBus.Publish(new UndockCompletedEvent(stationId));

                    // Defer structural change until after query iteration
                    shouldRemoveDocking = true;

                    // Clear flag
                    flags.ValueRW.UndockCompleted = false;
                }
            }

            // Structural changes must happen outside ALL query iterations
            if (shouldRemoveDocking)
            {
                // Collect entity first, then remove after query completes
                Entity shipToUndock = Entity.Null;
                foreach (var (docking, entity)
                    in SystemAPI.Query<RefRO<DockingStateComponent>>()
                        .WithAll<PlayerControlledTag>()
                        .WithEntityAccess())
                {
                    shipToUndock = entity;
                }

                if (shipToUndock != Entity.Null)
                {
                    EntityManager.RemoveComponent<DockingStateComponent>(shipToUndock);
                }
            }
        }
    }
}
