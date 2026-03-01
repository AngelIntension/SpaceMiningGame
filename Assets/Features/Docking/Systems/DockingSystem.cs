using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Ship.Data;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Docking.Systems
{
    /// <summary>
    /// Burst-compiled docking state machine. Manages approach → snap → docked → undocking phases.
    /// Runs before ShipPhysicsSystem so snap writes aren't overwritten.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Ship.Systems.ShipPhysicsSystem))]
    public partial struct DockingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerControlledTag>();

            // Ensure the DockingEventFlags singleton exists
            var flagsEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(flagsEntity, new DockingEventFlags());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Read DockingConfig values via blob or hardcoded defaults for Burst compatibility
            float snapDuration = 1.5f;
            float undockClearanceDistance = 100f;
            float undockDuration = 2f;
            float snapRange = 30f;
            float approachTimeout = 120f; // Safety timeout for approach phase (seconds)

            foreach (var (docking, position, velocity, flightMode, command)
                in SystemAPI.Query<
                    RefRW<DockingStateComponent>,
                    RefRW<ShipPositionComponent>,
                    RefRW<ShipVelocityComponent>,
                    RefRW<ShipFlightModeComponent>,
                    RefRO<PilotCommandComponent>>()
                    .WithAll<PlayerControlledTag>())
            {
                var phase = docking.ValueRO.Phase;

                switch (phase)
                {
                    case DockingPhase.Approaching:
                        HandleApproaching(ref state, ref docking.ValueRW, ref position.ValueRW,
                            ref velocity.ValueRW, ref flightMode.ValueRW, command.ValueRO,
                            snapRange, dt, approachTimeout);
                        break;

                    case DockingPhase.Snapping:
                        HandleSnapping(ref state, ref docking.ValueRW, ref position.ValueRW,
                            ref velocity.ValueRW, ref flightMode.ValueRW, dt, snapDuration);
                        break;

                    case DockingPhase.Docked:
                        HandleDocked(ref position.ValueRW, ref velocity.ValueRW,
                            ref flightMode.ValueRW, docking.ValueRO);
                        break;

                    case DockingPhase.Undocking:
                        HandleUndocking(ref state, ref docking.ValueRW, ref position.ValueRW,
                            ref velocity.ValueRW, ref flightMode.ValueRW, dt,
                            undockClearanceDistance, undockDuration);
                        break;
                }
            }
        }

        private void HandleApproaching(ref SystemState state,
            ref DockingStateComponent docking,
            ref ShipPositionComponent position,
            ref ShipVelocityComponent velocity,
            ref ShipFlightModeComponent flightMode,
            PilotCommandComponent command,
            float snapRange, float dt, float approachTimeout)
        {
            flightMode.Mode = ShipFlightMode.Docking;

            // Safety timeout: cancel if approach takes too long
            docking.SnapTimer += dt;
            if (docking.SnapTimer > approachTimeout)
            {
                // Timeout — reset to idle
                flightMode.Mode = ShipFlightMode.Idle;
                docking.Phase = DockingPhase.None;
                return;
            }

            // Validate target position (guard against degenerate data)
            if (math.any(math.isnan(docking.TargetPortPosition)) ||
                math.any(math.isinf(docking.TargetPortPosition)))
            {
                flightMode.Mode = ShipFlightMode.Idle;
                docking.Phase = DockingPhase.None;
                return;
            }

            // Check if within snap range
            if (DockingMath.IsWithinSnapRange(position.Position, docking.TargetPortPosition, snapRange))
            {
                // Transition to Snapping
                docking.Phase = DockingPhase.Snapping;
                docking.SnapTimer = 0f;
                docking.StartPosition = position.Position;
                docking.StartRotation = position.Rotation;

                // Zero velocity for smooth snap
                velocity.Velocity = float3.zero;
                velocity.AngularVelocity = float3.zero;
            }
            else
            {
                // Set align point to port position for existing approach autopilot
                // The approach is handled by writing to PilotCommandComponent
                // DockingSystem sets the target; ShipPhysicsSystem skips Docking mode
                // so we handle approach thrust directly here

                var toTarget = math.normalizesafe(docking.TargetPortPosition - position.Position);
                float distance = math.length(docking.TargetPortPosition - position.Position);

                // Approach: rotate toward port, thrust only when well-aligned
                var forward = math.forward(position.Rotation);
                var alignTorque = ShipPhysicsMath.ComputeAlignTorque(forward, toTarget, 5f);
                float dot = math.dot(forward, toTarget);

                // Only thrust when mostly facing target (>0.5) to prevent spiraling
                float alignment = math.saturate((dot - 0.5f) * 2f);
                float throttle = math.saturate(distance / 100f);
                var thrust = toTarget * alignment * throttle * 200f;

                velocity.Velocity += thrust * dt;
                velocity.Velocity = ShipPhysicsMath.ApplyDamping(velocity.Velocity, 1.5f, dt);
                velocity.Velocity = ShipPhysicsMath.ClampSpeed(velocity.Velocity, 100f);

                velocity.AngularVelocity += alignTorque * dt;
                velocity.AngularVelocity = ShipPhysicsMath.ApplyDamping(velocity.AngularVelocity, 5f, dt);

                position.Position += velocity.Velocity * dt;
                position.Rotation = ShipPhysicsMath.IntegrateRotation(position.Rotation, velocity.AngularVelocity, dt);
            }
        }

        private void HandleSnapping(ref SystemState state,
            ref DockingStateComponent docking,
            ref ShipPositionComponent position,
            ref ShipVelocityComponent velocity,
            ref ShipFlightModeComponent flightMode,
            float dt, float snapDuration)
        {
            flightMode.Mode = ShipFlightMode.Docking;
            docking.SnapTimer += dt;

            float t = DockingMath.ComputeSnapProgress(docking.SnapTimer, snapDuration);
            var (pos, rot) = DockingMath.InterpolateSnapPose(
                docking.StartPosition, docking.StartRotation,
                docking.TargetPortPosition, docking.TargetPortRotation, t);

            position.Position = pos;
            position.Rotation = rot;
            velocity.Velocity = float3.zero;
            velocity.AngularVelocity = float3.zero;

            if (t >= 1f)
            {
                // Transition to Docked
                docking.Phase = DockingPhase.Docked;
                position.Position = docking.TargetPortPosition;
                position.Rotation = docking.TargetPortRotation;
                flightMode.Mode = ShipFlightMode.Docked;

                // Write dock completion flag for bridge system
                SetDockCompletedFlag(ref state, docking.TargetStationId);
            }
        }

        private static void HandleDocked(
            ref ShipPositionComponent position,
            ref ShipVelocityComponent velocity,
            ref ShipFlightModeComponent flightMode,
            DockingStateComponent docking)
        {
            // Lock ship at docking port
            position.Position = docking.TargetPortPosition;
            position.Rotation = docking.TargetPortRotation;
            velocity.Velocity = float3.zero;
            velocity.AngularVelocity = float3.zero;
            flightMode.Mode = ShipFlightMode.Docked;
        }

        private void HandleUndocking(ref SystemState state,
            ref DockingStateComponent docking,
            ref ShipPositionComponent position,
            ref ShipVelocityComponent velocity,
            ref ShipFlightModeComponent flightMode,
            float dt, float clearanceDistance, float undockDuration)
        {
            flightMode.Mode = ShipFlightMode.Docking;
            docking.SnapTimer += dt;

            // Compute clearance direction (away from port, using ship's forward)
            var portForward = math.forward(docking.TargetPortRotation);
            var clearancePos = DockingMath.ComputeClearancePosition(
                docking.TargetPortPosition, portForward, clearanceDistance);

            float t = DockingMath.ComputeSnapProgress(docking.SnapTimer, undockDuration);
            var (pos, rot) = DockingMath.InterpolateSnapPose(
                docking.TargetPortPosition, docking.TargetPortRotation,
                clearancePos, docking.TargetPortRotation, t);

            position.Position = pos;
            position.Rotation = rot;
            velocity.Velocity = float3.zero;
            velocity.AngularVelocity = float3.zero;

            if (t >= 1f)
            {
                // Transition complete — write undock flag for bridge system
                flightMode.Mode = ShipFlightMode.Idle;
                SetUndockCompletedFlag(ref state);
            }
        }

        private void SetDockCompletedFlag(ref SystemState state, int stationId)
        {
            foreach (var flags in SystemAPI.Query<RefRW<DockingEventFlags>>())
            {
                flags.ValueRW.DockCompleted = true;
                flags.ValueRW.DockStationId = stationId;
            }
        }

        private void SetUndockCompletedFlag(ref SystemState state)
        {
            foreach (var flags in SystemAPI.Query<RefRW<DockingEventFlags>>())
            {
                flags.ValueRW.UndockCompleted = true;
            }
        }
    }
}
