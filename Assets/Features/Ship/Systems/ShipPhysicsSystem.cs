using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Ship.Systems
{
    /// <summary>
    /// Burst-compiled ship physics simulation. Reads pilot commands, applies Newtonian physics.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ShipPhysicsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerControlledTag>();
            state.RequireForUpdate<PilotCommandComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (position, velocity, config, flightMode, command, tag)
                in SystemAPI.Query<
                    RefRW<ShipPositionComponent>,
                    RefRW<ShipVelocityComponent>,
                    RefRO<ShipConfigComponent>,
                    RefRW<ShipFlightModeComponent>,
                    RefRO<PilotCommandComponent>,
                    RefRO<PlayerControlledTag>>())
            {
                var cmd = command.ValueRO;
                var cfg = config.ValueRO;
                var pos = position.ValueRO;
                var vel = velocity.ValueRO;

                // Determine flight mode
                var newMode = ShipPhysicsMath.DetermineFlightMode(
                    flightMode.ValueRO.Mode,
                    cmd.Forward, cmd.Strafe, cmd.Roll,
                    cmd.HasAlignPoint, cmd.RadialAction);

                // Get local axes from current rotation
                var forward = math.forward(pos.Rotation);
                var right = math.mul(pos.Rotation, math.right());
                var up = math.mul(pos.Rotation, math.up());

                // Compute forces — manual or auto-pilot
                float3 thrust;
                float3 torque;

                if (newMode == ShipFlightMode.ManualThrust || newMode == ShipFlightMode.Idle)
                {
                    // Manual: pilot input drives thrust and torque
                    thrust = ShipPhysicsMath.ComputeThrust(
                        forward, right, up,
                        cmd.Forward, cmd.Strafe, cmd.Roll,
                        cfg.MaxThrust, newMode);

                    torque = ShipPhysicsMath.ComputeTorque(
                        forward, up,
                        cmd.Forward, cmd.Strafe, cmd.Roll,
                        cfg.RotationTorque, newMode);
                }
                else
                {
                    // Auto-pilot: compute based on flight mode and target
                    var toTarget = cmd.HasAlignPoint
                        ? math.normalizesafe(cmd.AlignPoint - pos.Position)
                        : forward;

                    float distance = cmd.HasAlignPoint
                        ? math.length(cmd.AlignPoint - pos.Position)
                        : 0f;

                    // All auto-pilot modes use align torque toward target
                    torque = ShipPhysicsMath.ComputeAlignTorque(forward, toTarget, cfg.RotationTorque);

                    thrust = newMode switch
                    {
                        ShipFlightMode.AlignToPoint => float3.zero, // Align only, no thrust
                        ShipFlightMode.Approach => ShipPhysicsMath.ComputeApproachThrust(
                            forward, toTarget, distance, cmd.RadialDistance, cfg.MaxThrust),
                        ShipFlightMode.Orbit => ShipPhysicsMath.ComputeOrbitThrust(
                            forward, toTarget, distance, cmd.RadialDistance, cfg.MaxThrust),
                        ShipFlightMode.KeepAtRange => ShipPhysicsMath.ComputeKeepAtRangeThrust(
                            toTarget, distance, cmd.RadialDistance, cfg.MaxThrust),
                        _ => float3.zero
                    };
                }

                // Apply physics
                var newVelocity = ShipPhysicsMath.ApplyForce(vel.Velocity, thrust, cfg.Mass, dt);
                newVelocity = ShipPhysicsMath.ApplyDamping(newVelocity, cfg.LinearDamping, dt);
                newVelocity = ShipPhysicsMath.ClampSpeed(newVelocity, cfg.MaxSpeed);
                newVelocity = ShipPhysicsMath.SanitizeVelocity(newVelocity);

                var newAngVelocity = ShipPhysicsMath.ApplyForce(vel.AngularVelocity, torque, cfg.Mass, dt);
                newAngVelocity = ShipPhysicsMath.ApplyDamping(newAngVelocity, cfg.AngularDamping, dt);
                newAngVelocity = ShipPhysicsMath.SanitizeVelocity(newAngVelocity);

                // Integrate
                var newPosition = pos.Position + newVelocity * dt;
                var newRotation = ShipPhysicsMath.IntegrateRotation(pos.Rotation, newAngVelocity, dt);

                // Write back
                position.ValueRW.Position = newPosition;
                position.ValueRW.Rotation = newRotation;
                velocity.ValueRW.Velocity = newVelocity;
                velocity.ValueRW.AngularVelocity = newAngVelocity;
                flightMode.ValueRW.Mode = newMode;
            }
        }
    }
}
