using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Ship.Systems
{
    /// <summary>
    /// Store-level ship state reducer. Thin projection layer - no physics computation.
    /// Only handles SyncShipPhysicsAction from ECS-to-Store sync.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public static class ShipStateReducer
    {
        /// <summary>
        /// Reduce ship state by applying a ship action (projection from ECS sync). See MVP-01: 6DOF Newtonian flight.
        /// </summary>
        public static ShipState Reduce(ShipState state, IShipAction action)
            => action switch
            {
                SyncShipPhysicsAction a => state with
                {
                    Position = a.Position,
                    Rotation = a.Rotation,
                    Velocity = a.Velocity,
                    AngularVelocity = a.AngularVelocity,
                    FlightMode = a.FlightMode
                },
                RepairHullAction a => state with { HullIntegrity = a.NewIntegrity },
                _ => state
            };
    }
}
