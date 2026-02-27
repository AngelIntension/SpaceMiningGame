using Unity.Mathematics;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Projected from ECS ShipPhysicsSystem via EcsToStoreSyncSystem.
    /// One-way ECS→Store sync for HUD/view consumption.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public sealed record SyncShipPhysicsAction(
        float3 Position,
        quaternion Rotation,
        float3 Velocity,
        float3 AngularVelocity,
        ShipFlightMode FlightMode
    ) : IShipAction;
}
