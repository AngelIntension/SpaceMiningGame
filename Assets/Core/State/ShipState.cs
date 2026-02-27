using Unity.Mathematics;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Immutable ship physics state. Projected from ECS via SyncShipPhysicsAction.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public sealed record ShipState(
        float3 Position,
        quaternion Rotation,
        float3 Velocity,
        float3 AngularVelocity,
        float Mass,
        float MaxThrust,
        float MaxSpeed,
        float RotationTorque,
        float LinearDamping,
        float AngularDamping,
        ShipFlightMode FlightMode,
        float HullIntegrity
    )
    {
        public static readonly ShipState Default = new(
            float3.zero,
            quaternion.identity,
            float3.zero,
            float3.zero,
            Mass: 1000f,
            MaxThrust: 5000f,
            MaxSpeed: 100f,
            RotationTorque: 50f,
            LinearDamping: 0.5f,
            AngularDamping: 2.0f,
            ShipFlightMode.Idle,
            HullIntegrity: 1.0f
        );
    }
}
