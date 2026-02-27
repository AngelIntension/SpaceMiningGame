using Unity.Entities;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Ship.Systems
{
    /// <summary>
    /// ECS-to-Store sync. Projects ship physics into StateStore for HUD/view consumption.
    /// Runs at end of SimulationSystemGroup.
    /// // CONSTITUTION DEVIATION: DOTS SystemBase cannot use constructor injection
    /// See research.md § R7.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class EcsToStoreSyncSystem : SystemBase
    {
        private static IStateStore _stateStore;

        /// <summary>
        /// Set the state store reference. Called once during initialization from managed code.
        /// // CONSTITUTION DEVIATION: DOTS SystemBase cannot use constructor injection
        /// </summary>
        public static void SetStateStore(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        /// <summary>
        /// Project player ship physics from ECS into StateStore each frame. See MVP-01: 6DOF Newtonian flight.
        /// </summary>
        protected override void OnUpdate()
        {
            if (_stateStore == null) return;

            Entities
                .WithAll<PlayerControlledTag>()
                .ForEach((
                    in ShipPositionComponent pos,
                    in ShipVelocityComponent vel,
                    in ShipFlightModeComponent mode) =>
                {
                    _stateStore.Dispatch(new SyncShipPhysicsAction(
                        pos.Position,
                        pos.Rotation,
                        vel.Velocity,
                        vel.AngularVelocity,
                        mode.Mode
                    ));
                })
                .WithoutBurst()
                .Run();
        }
    }
}
