using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Ship.Systems
{
    /// <summary>
    /// Pure static functions for ship physics. Burst-compatible (unmanaged types only).
    /// Called by both ShipPhysicsSystem (ECS) and unit tests.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public static class ShipPhysicsMath
    {
        /// <summary>
        /// Determine flight mode based on inputs. Manual override always wins.
        /// </summary>
        public static ShipFlightMode DetermineFlightMode(
            ShipFlightMode current,
            float forward, float strafe, float roll,
            bool hasAlignPoint, int radialAction)
        {
            // Manual thrust input always overrides auto-pilot
            bool hasManualInput = math.abs(forward) > 0.01f ||
                                  math.abs(strafe) > 0.01f ||
                                  math.abs(roll) > 0.01f;

            if (hasManualInput)
                return ShipFlightMode.ManualThrust;

            // Radial menu action takes priority over align
            if (radialAction >= 0)
            {
                return radialAction switch
                {
                    0 => ShipFlightMode.Approach,
                    1 => ShipFlightMode.Orbit,
                    3 => ShipFlightMode.KeepAtRange,
                    _ => current
                };
            }

            // Double-click align
            if (hasAlignPoint)
                return ShipFlightMode.AlignToPoint;

            // No input and currently thrusting -> decay to idle
            if (current == ShipFlightMode.ManualThrust)
                return ShipFlightMode.Idle;

            return current;
        }

        /// <summary>
        /// Compute world-space thrust force from pilot input.
        /// </summary>
        public static float3 ComputeThrust(
            float3 localForward, float3 localRight, float3 localUp,
            float forward, float strafe, float roll,
            float maxThrust, ShipFlightMode mode)
        {
            if (mode != ShipFlightMode.ManualThrust && mode != ShipFlightMode.Idle)
                return float3.zero; // Auto-pilot modes handle their own thrust

            var force = localForward * forward * maxThrust
                      + localRight * strafe * maxThrust;
            // Roll doesn't produce linear force, only torque
            return force;
        }

        /// <summary>
        /// Compute world-space torque from pilot input.
        /// </summary>
        public static float3 ComputeTorque(
            float3 localForward, float3 localUp,
            float forward, float strafe, float roll,
            float rotationTorque, ShipFlightMode mode)
        {
            if (mode != ShipFlightMode.ManualThrust && mode != ShipFlightMode.Idle)
                return float3.zero;

            // Roll around forward axis (negated so Q=left, E=right matches visual expectation)
            return localForward * -roll * rotationTorque;
        }

        /// <summary>
        /// Apply force to velocity: v' = v + (F/m) * dt.
        /// Zero mass guard: returns unchanged velocity.
        /// </summary>
        public static float3 ApplyForce(float3 velocity, float3 force, float mass, float dt)
        {
            if (mass <= 0f) return velocity;
            return velocity + (force / mass) * dt;
        }

        /// <summary>
        /// Apply damping: v' = v * (1 - damping * dt), clamped to non-negative magnitude.
        /// </summary>
        public static float3 ApplyDamping(float3 velocity, float damping, float dt)
        {
            var factor = math.max(0f, 1f - damping * dt);
            return velocity * factor;
        }

        /// <summary>
        /// Clamp velocity magnitude to maxSpeed.
        /// </summary>
        public static float3 ClampSpeed(float3 velocity, float maxSpeed)
        {
            var speed = math.length(velocity);
            if (speed <= maxSpeed || speed < 0.0001f) return velocity;
            return math.normalize(velocity) * maxSpeed;
        }

        /// <summary>
        /// Integrate rotation: q' = normalize(q + 0.5 * dt * omega * q).
        /// Uses quaternion differential equation for smooth rotation.
        /// </summary>
        public static quaternion IntegrateRotation(quaternion rotation, float3 angularVelocity, float dt)
        {
            if (math.lengthsq(angularVelocity) < 0.000001f)
                return rotation;

            var omega = new quaternion(
                angularVelocity.x * 0.5f * dt,
                angularVelocity.y * 0.5f * dt,
                angularVelocity.z * 0.5f * dt,
                0f
            );

            var q = rotation.value;
            var dq = math.mul(omega, rotation).value;
            var newQ = new float4(q.x + dq.x, q.y + dq.y, q.z + dq.z, q.w + dq.w);
            return math.normalizesafe(new quaternion(newQ));
        }

        /// <summary>
        /// Guard: clamp NaN velocity to zero.
        /// </summary>
        public static float3 SanitizeVelocity(float3 velocity)
        {
            if (math.any(math.isnan(velocity)))
                return float3.zero;
            return velocity;
        }

        // ──────────────────────────────────────────────
        // Auto-pilot flight mode functions (US2)
        // See MVP-03: Target selection, MVP-04: Radial menu
        // ──────────────────────────────────────────────

        private const float AlignTolerance = 0.01f;
        private const float DistanceTolerance = 1f;

        /// <summary>
        /// Compute torque to rotate ship toward a target direction.
        /// Uses cross product for rotation axis, scaled by rotationTorque.
        /// </summary>
        public static float3 ComputeAlignTorque(float3 currentForward, float3 toTarget, float rotationTorque)
        {
            var cross = math.cross(currentForward, toTarget);
            float sinAngle = math.length(cross);

            if (sinAngle < AlignTolerance)
                return float3.zero;

            var axis = cross / sinAngle; // normalized rotation axis
            return axis * sinAngle * rotationTorque;
        }

        /// <summary>
        /// Compute thrust for Approach mode. Forward thrust when aligned and far from target.
        /// Decelerates proportionally as distance approaches targetDistance.
        /// See MVP-03: double-click align + approach.
        /// </summary>
        public static float3 ComputeApproachThrust(
            float3 currentForward, float3 toTarget, float distance, float targetDistance, float maxThrust)
        {
            float distanceDelta = distance - targetDistance;
            if (distanceDelta <= DistanceTolerance)
                return float3.zero;

            // Alignment factor: dot product [0,1] — reduces thrust when misaligned
            float alignment = math.max(0f, math.dot(currentForward, toTarget));

            // Distance-based throttle: ramp down as we approach target distance
            float throttle = math.saturate(distanceDelta / math.max(targetDistance, 1f));

            return currentForward * alignment * throttle * maxThrust;
        }

        /// <summary>
        /// Compute thrust for Orbit mode. Lateral thrust perpendicular to target direction
        /// plus radial correction to maintain targetDistance.
        /// See MVP-04: Orbit radial action.
        /// </summary>
        public static float3 ComputeOrbitThrust(
            float3 currentForward, float3 toTarget, float distance, float targetDistance, float maxThrust)
        {
            // Lateral (tangential) component: perpendicular to toTarget in the horizontal plane
            var tangent = math.cross(toTarget, math.up());
            if (math.lengthsq(tangent) < 0.0001f)
                tangent = math.cross(toTarget, math.right());
            tangent = math.normalize(tangent);

            float lateralThrust = maxThrust * 0.5f;
            var lateral = tangent * lateralThrust;

            // Radial correction: move toward or away to maintain target distance
            float distanceDelta = distance - targetDistance;
            float radialFactor = math.saturate(math.abs(distanceDelta) / math.max(targetDistance, 1f));
            var radial = toTarget * math.sign(distanceDelta) * radialFactor * maxThrust * 0.5f;

            return lateral + radial;
        }

        /// <summary>
        /// Compute thrust for KeepAtRange mode. Forward/reverse only along toTarget axis.
        /// No lateral orbiting — purely radial distance maintenance.
        /// See MVP-04: KeepAtRange radial action.
        /// </summary>
        public static float3 ComputeKeepAtRangeThrust(
            float3 toTarget, float distance, float targetDistance, float maxThrust)
        {
            float distanceDelta = distance - targetDistance;
            if (math.abs(distanceDelta) <= DistanceTolerance)
                return float3.zero;

            float throttle = math.saturate(math.abs(distanceDelta) / math.max(targetDistance, 1f));
            return toTarget * math.sign(distanceDelta) * throttle * maxThrust;
        }
    }
}
