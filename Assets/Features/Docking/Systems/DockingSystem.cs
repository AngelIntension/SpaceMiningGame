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
    /// Burst-compiled docking state machine. Manages approach → align → snap → docked → undocking phases.
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

            // Require DockingConfigBlobComponent singleton before running
            state.RequireForUpdate<DockingConfigBlobComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Read all docking parameters from the baked DockingConfigBlob singleton
            ref var cfg = ref SystemAPI.GetSingleton<DockingConfigBlobComponent>().Config.Value;
            float snapDuration = cfg.SnapDuration;
            float undockClearanceDistance = cfg.UndockClearanceDistance;
            float snapRange = cfg.SnapRange;
            float approachTimeout = cfg.ApproachTimeout;
            float alignTimeout = cfg.AlignTimeout;
            float alignDotThreshold = cfg.AlignDotThreshold;
            float alignAngVelThreshold = cfg.AlignAngVelThreshold;

            foreach (var (docking, position, velocity, flightMode, command, config)
                in SystemAPI.Query<
                    RefRW<DockingStateComponent>,
                    RefRW<ShipPositionComponent>,
                    RefRW<ShipVelocityComponent>,
                    RefRW<ShipFlightModeComponent>,
                    RefRO<PilotCommandComponent>,
                    RefRO<ShipConfigComponent>>()
                    .WithAll<PlayerControlledTag>())
            {
                var phase = docking.ValueRO.Phase;

                switch (phase)
                {
                    case DockingPhase.Approaching:
                        HandleApproaching(ref state, ref docking.ValueRW, ref position.ValueRW,
                            ref velocity.ValueRW, ref flightMode.ValueRW, command.ValueRO,
                            config.ValueRO, snapRange, dt, approachTimeout);
                        break;

                    case DockingPhase.Aligning:
                        HandleAligning(ref docking.ValueRW, ref position.ValueRW,
                            ref velocity.ValueRW, ref flightMode.ValueRW,
                            config.ValueRO, dt, alignTimeout, alignDotThreshold, alignAngVelThreshold);
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
                            ref velocity.ValueRW, ref flightMode.ValueRW,
                            config.ValueRO, dt, undockClearanceDistance, approachTimeout);
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
            ShipConfigComponent config,
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
                // Transition to Aligning — rotate to match port orientation using ship physics
                docking.Phase = DockingPhase.Aligning;
                docking.SnapTimer = 0f;

                // Zero linear velocity; alignment is rotation-only with station-keeping
                velocity.Velocity = float3.zero;
            }
            else
            {
                // Approach uses the ship's actual physics parameters (mass, thrust, damping)
                var toTarget = math.normalizesafe(docking.TargetPortPosition - position.Position);
                float distance = math.length(docking.TargetPortPosition - position.Position);

                // Rotate toward docking port
                var forward = math.forward(position.Rotation);
                var alignTorque = ShipPhysicsMath.ComputeAlignTorque(forward, toTarget, config.RotationTorque);
                float dot = math.dot(forward, toTarget);

                // Only thrust when mostly facing target (>0.5) to prevent spiraling
                float alignment = math.saturate((dot - 0.5f) * 2f);

                // Braking: compute stopping distance at current speed given max deceleration
                float speed = math.length(velocity.Velocity);
                float maxDecel = config.MaxThrust / math.max(config.Mass, 0.001f);
                // v² / (2a) = distance needed to stop; add snap range as target stop point
                float stoppingDistance = (speed * speed) / (2f * math.max(maxDecel, 0.001f)) + snapRange;

                // Throttle: full thrust when far, brake when within stopping distance
                float throttle;
                if (distance > stoppingDistance)
                {
                    // Cruise: accelerate toward target
                    throttle = alignment;
                }
                else
                {
                    // Brake: reduce thrust proportionally, go negative to decelerate
                    float brakeFactor = math.saturate((distance - snapRange) / math.max(stoppingDistance - snapRange, 0.001f));
                    throttle = alignment * brakeFactor;
                }

                var thrustForce = toTarget * throttle * config.MaxThrust;

                // Apply physics using ship's actual parameters (F = ma → a = F/m)
                velocity.Velocity = ShipPhysicsMath.ApplyForce(velocity.Velocity, thrustForce, config.Mass, dt);
                velocity.Velocity = ShipPhysicsMath.ApplyDamping(velocity.Velocity, config.LinearDamping, dt);
                velocity.Velocity = ShipPhysicsMath.ClampSpeed(velocity.Velocity, config.MaxSpeed);
                velocity.Velocity = ShipPhysicsMath.SanitizeVelocity(velocity.Velocity);

                velocity.AngularVelocity = ShipPhysicsMath.ApplyForce(velocity.AngularVelocity, alignTorque, config.Mass, dt);
                velocity.AngularVelocity = ShipPhysicsMath.ApplyDamping(velocity.AngularVelocity, config.AngularDamping, dt);
                velocity.AngularVelocity = ShipPhysicsMath.SanitizeVelocity(velocity.AngularVelocity);

                position.Position += velocity.Velocity * dt;
                position.Rotation = ShipPhysicsMath.IntegrateRotation(position.Rotation, velocity.AngularVelocity, dt);
            }
        }

        private static void HandleAligning(
            ref DockingStateComponent docking,
            ref ShipPositionComponent position,
            ref ShipVelocityComponent velocity,
            ref ShipFlightModeComponent flightMode,
            ShipConfigComponent config,
            float dt, float alignTimeout, float dotThreshold, float angVelThreshold)
        {
            flightMode.Mode = ShipFlightMode.Docking;
            docking.SnapTimer += dt;

            // Safety timeout: force transition to snapping if alignment takes too long
            if (docking.SnapTimer > alignTimeout)
            {
                docking.Phase = DockingPhase.Snapping;
                docking.SnapTimer = 0f;
                docking.StartPosition = position.Position;
                docking.StartRotation = position.Rotation;
                velocity.AngularVelocity = float3.zero;
                return;
            }

            // Station-keeping: gentle thrust to hold position near the dock
            var toPort = docking.TargetPortPosition - position.Position;
            float drift = math.length(toPort);
            if (drift > 0.1f)
            {
                var holdForce = math.normalizesafe(toPort) * math.min(drift, 1f) * config.MaxThrust * 0.3f;
                velocity.Velocity = ShipPhysicsMath.ApplyForce(velocity.Velocity, holdForce, config.Mass, dt);
            }
            velocity.Velocity = ShipPhysicsMath.ApplyDamping(velocity.Velocity, config.LinearDamping * 3f, dt);
            velocity.Velocity = ShipPhysicsMath.SanitizeVelocity(velocity.Velocity);
            position.Position += velocity.Velocity * dt;

            // Rotation: align both forward and up axes to match docking port orientation
            var targetForward = math.forward(docking.TargetPortRotation);
            var targetUp = math.mul(docking.TargetPortRotation, math.up());
            var currentForward = math.forward(position.Rotation);
            var currentUp = math.mul(position.Rotation, math.up());

            // Primary torque: align forward direction
            var forwardTorque = ShipPhysicsMath.ComputeAlignTorque(currentForward, targetForward, config.RotationTorque);
            // Secondary torque: correct roll (half strength to prioritize yaw/pitch)
            var upTorque = ShipPhysicsMath.ComputeAlignTorque(currentUp, targetUp, config.RotationTorque * 0.5f);

            var totalTorque = forwardTorque + upTorque;
            velocity.AngularVelocity = ShipPhysicsMath.ApplyForce(velocity.AngularVelocity, totalTorque, config.Mass, dt);
            velocity.AngularVelocity = ShipPhysicsMath.ApplyDamping(velocity.AngularVelocity, config.AngularDamping, dt);
            velocity.AngularVelocity = ShipPhysicsMath.SanitizeVelocity(velocity.AngularVelocity);
            position.Rotation = ShipPhysicsMath.IntegrateRotation(position.Rotation, velocity.AngularVelocity, dt);

            // Check alignment completion: both axes aligned and angular velocity settled
            float forwardDot = math.dot(currentForward, targetForward);
            float upDot = math.dot(currentUp, targetUp);
            float angVelMag = math.length(velocity.AngularVelocity);

            if (forwardDot > dotThreshold && upDot > dotThreshold && angVelMag < angVelThreshold)
            {
                // Aligned — transition to final snap lock-in
                docking.Phase = DockingPhase.Snapping;
                docking.SnapTimer = 0f;
                docking.StartPosition = position.Position;
                docking.StartRotation = position.Rotation;
                velocity.Velocity = float3.zero;
                velocity.AngularVelocity = float3.zero;
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
            ShipConfigComponent config,
            float dt, float clearanceDistance, float undockTimeout)
        {
            flightMode.Mode = ShipFlightMode.Docking;
            docking.SnapTimer += dt;

            // Safety timeout: force completion if undocking takes too long
            if (docking.SnapTimer > undockTimeout)
            {
                velocity.Velocity = float3.zero;
                velocity.AngularVelocity = float3.zero;
                flightMode.Mode = ShipFlightMode.Idle;
                SetUndockCompletedFlag(ref state);
                return;
            }

            // Departure direction: away from the port along its forward axis
            var departDirection = math.forward(docking.TargetPortRotation);
            float distanceFromDock = math.length(position.Position - docking.TargetPortPosition);

            // Braking: compute stopping distance at current speed
            float speed = math.length(velocity.Velocity);
            float maxDecel = config.MaxThrust / math.max(config.Mass, 0.001f);
            float stoppingDistance = (speed * speed) / (2f * math.max(maxDecel, 0.001f));
            float distanceRemaining = clearanceDistance - distanceFromDock;

            float throttle;
            if (distanceRemaining > stoppingDistance)
            {
                // Cruise: accelerate away from station
                throttle = 1f;
            }
            else
            {
                // Brake: decelerate to stop at clearance distance
                throttle = math.saturate(distanceRemaining / math.max(stoppingDistance, 0.001f));
            }

            var thrustForce = departDirection * throttle * config.MaxThrust;

            // Apply physics using ship's actual parameters
            velocity.Velocity = ShipPhysicsMath.ApplyForce(velocity.Velocity, thrustForce, config.Mass, dt);
            velocity.Velocity = ShipPhysicsMath.ApplyDamping(velocity.Velocity, config.LinearDamping, dt);
            velocity.Velocity = ShipPhysicsMath.ClampSpeed(velocity.Velocity, config.MaxSpeed);
            velocity.Velocity = ShipPhysicsMath.SanitizeVelocity(velocity.Velocity);

            // Maintain docking rotation during departure (no angular drift)
            velocity.AngularVelocity = float3.zero;

            position.Position += velocity.Velocity * dt;

            // Clearance reached and nearly stopped — undocking complete
            if (distanceFromDock >= clearanceDistance && speed < 1f)
            {
                velocity.Velocity = float3.zero;
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
