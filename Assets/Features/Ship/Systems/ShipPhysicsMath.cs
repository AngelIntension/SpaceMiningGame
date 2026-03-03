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

            // Docked mode: ship is locked until undock action
            if (current == ShipFlightMode.Docked)
                return ShipFlightMode.Docked;

            // Docking mode: manual input or radial action cancels docking
            if (current == ShipFlightMode.Docking)
            {
                if (hasManualInput)
                    return ShipFlightMode.ManualThrust;
                if (radialAction >= 0)
                {
                    return radialAction switch
                    {
                        0 => ShipFlightMode.Approach,
                        1 => ShipFlightMode.Orbit,
                        3 => ShipFlightMode.KeepAtRange,
                        _ => ShipFlightMode.Docking
                    };
                }
                return ShipFlightMode.Docking;
            }

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
            {
                // Anti-parallel: pick arbitrary perpendicular axis to break symmetry
                float dot = math.dot(currentForward, toTarget);
                if (dot < -0.9f)
                {
                    var up = math.abs(currentForward.y) < 0.9f
                        ? new float3(0, 1, 0)
                        : new float3(1, 0, 0);
                    var perp = math.normalizesafe(math.cross(currentForward, up));
                    return perp * rotationTorque;
                }
                return float3.zero;
            }

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
        /// Compute the maximum sustainable orbital speed given thrust budget, damping, and radius.
        /// At equilibrium: F_damping = m*v*damping, F_centripetal = m*v²/R.
        /// Constraint: sqrt(F_damping² + F_centripetal²) &lt;= maxThrust.
        /// Solves via 3 Newton iterations.
        /// </summary>
        public static float ComputeMaxOrbitalSpeed(
            float maxThrust, float mass, float linearDamping, float maxSpeed, float orbitRadius)
        {
            if (mass <= 0f || orbitRadius <= 0f || maxThrust <= 0f)
                return 0f;

            // Initial guess: damping-only equilibrium (no centripetal cost)
            float v = maxThrust / math.max(mass * linearDamping, 0.0001f);
            float thrustSq = maxThrust * maxThrust;

            // Newton iterations: solve f(v) = (m*v*d)² + (m*v²/R)² - F_max² = 0
            for (int i = 0; i < 3; i++)
            {
                float fd = mass * v * linearDamping;
                float fc = mass * v * v / orbitRadius;
                float fSq = fd * fd + fc * fc;
                float f = fSq - thrustSq;

                // Derivative: df/dv = 2*fd*(m*d) + 2*fc*(2*m*v/R)
                float dfDv = 2f * fd * (mass * linearDamping) + 2f * fc * (2f * mass * v / orbitRadius);

                if (math.abs(dfDv) < 0.0001f)
                    break;

                v -= f / dfDv;
                v = math.max(v, 0f);
            }

            return math.min(v, maxSpeed);
        }

        /// <summary>
        /// Compute the geometric tangent point on the orbit circle from the ship's position.
        /// When outside the circle: uses acos(R/d) geometric tangent.
        /// When inside or on circle: returns nearest point on circle edge (project outward).
        /// Orbit plane is horizontal (world up normal).
        /// </summary>
        public static float3 ComputeOrbitTangentPoint(float3 shipPosition, float3 orbitCenter, float orbitRadius)
        {
            var toShip = shipPosition - orbitCenter;
            // Project to horizontal plane (zero out Y)
            var toShipFlat = new float3(toShip.x, 0f, toShip.z);
            float flatDist = math.length(toShipFlat);

            if (flatDist < 0.0001f)
            {
                // Ship is directly above/below center — pick arbitrary direction
                return orbitCenter + new float3(orbitRadius, 0f, 0f);
            }

            var dirFromCenter = toShipFlat / flatDist;

            if (flatDist <= orbitRadius)
            {
                // Inside or on circle: return nearest point on circle edge
                return orbitCenter + dirFromCenter * orbitRadius + new float3(0f, shipPosition.y - orbitCenter.y, 0f);
            }

            // Outside circle: geometric tangent point
            // Angle from center-to-ship line to tangent line = acos(R/d)
            float tangentAngle = math.acos(math.clamp(orbitRadius / flatDist, -1f, 1f));

            // Rotate dirFromCenter by tangentAngle (clockwise for consistent orbit direction)
            float cosA = math.cos(tangentAngle);
            float sinA = math.sin(tangentAngle);
            var tangentDir = new float3(
                dirFromCenter.x * cosA + dirFromCenter.z * sinA,
                0f,
                -dirFromCenter.x * sinA + dirFromCenter.z * cosA);

            return orbitCenter + tangentDir * orbitRadius + new float3(0f, shipPosition.y - orbitCenter.y, 0f);
        }

        /// <summary>
        /// Returns true if the ship is close enough to the orbital circle to enter stable orbit.
        /// Threshold: max(5, targetDistance * 0.1) — 10% of radius or 5m minimum.
        /// </summary>
        public static bool IsOnOrbitalCircle(float distance, float targetDistance)
        {
            float threshold = math.max(5f, targetDistance * 0.1f);
            return math.abs(distance - targetDistance) <= threshold;
        }

        /// <summary>
        /// Two-phase orbit thrust: tangent approach when far from orbital circle,
        /// stable orbit with centripetal compensation + PD radial correction when on circle.
        /// </summary>
        public static float3 ComputeOrbitThrust(
            float3 shipPosition, float3 orbitCenter, float3 velocity, float distance,
            float targetDistance, float maxThrust, float mass, float linearDamping, float maxSpeed)
        {
            if (distance < 0.0001f || targetDistance < 0.0001f)
                return float3.zero;

            var toCenter = (orbitCenter - shipPosition) / distance;

            if (IsOnOrbitalCircle(distance, targetDistance))
            {
                // Phase 2: Stable Orbit
                // Tangent direction (perpendicular to toCenter in horizontal plane)
                var tangent = math.cross(toCenter, math.up());
                if (math.lengthsq(tangent) < 0.0001f)
                    tangent = math.cross(toCenter, math.right());
                tangent = math.normalize(tangent);

                float vTangential = math.dot(velocity, tangent);
                float vRadial = math.dot(velocity, toCenter);

                // Centripetal baseline: F_c = m * v_t² / r (toward center)
                float centripetal = mass * vTangential * vTangential / math.max(distance, 1f);
                var centripetalForce = toCenter * centripetal;

                // PD radial correction: Kp * radiusError - Kd * v_radial
                float radiusError = distance - targetDistance;
                const float Kp = 2.0f;
                const float Kd = 1.5f;
                float radialCorrection = Kp * radiusError - Kd * vRadial;
                var radialForce = toCenter * radialCorrection * mass;

                // Tangential thrust: drive toward max orbital speed
                float targetSpeed = ComputeMaxOrbitalSpeed(maxThrust, mass, linearDamping, maxSpeed, targetDistance);
                float speedError = targetSpeed - math.abs(vTangential);
                float tangentialThrust = math.clamp(speedError * mass * 2f, -maxThrust, maxThrust);
                var tangentialForce = tangent * tangentialThrust;

                var total = centripetalForce + radialForce + tangentialForce;

                // Clamp total to maxThrust
                float totalMag = math.length(total);
                if (totalMag > maxThrust)
                    total = total * (maxThrust / totalMag);

                return total;
            }
            else
            {
                // Phase 1: Tangent Approach
                var tangentPoint = ComputeOrbitTangentPoint(shipPosition, orbitCenter, targetDistance);
                var toTangent = tangentPoint - shipPosition;
                float tangentDist = math.length(toTangent);

                if (tangentDist < 0.0001f)
                    return float3.zero;

                var tangentDir = toTangent / tangentDist;

                // Alignment factor reduces thrust when velocity is misaligned with approach
                float speed = math.length(velocity);
                float alignment = speed > 0.1f
                    ? math.max(0f, math.dot(math.normalize(velocity), tangentDir))
                    : 1f;

                // Distance-based throttle
                float throttle = math.saturate(tangentDist / math.max(targetDistance, 1f));

                var approachForce = tangentDir * alignment * throttle * maxThrust;

                // Secondary radial correction toward orbital radius
                float radiusError = distance - targetDistance;
                float radialFactor = math.saturate(math.abs(radiusError) / math.max(targetDistance, 1f));
                var radialForce = toCenter * math.sign(radiusError) * radialFactor * maxThrust * 0.3f;

                var total = approachForce + radialForce;
                float totalMag = math.length(total);
                if (totalMag > maxThrust)
                    total = total * (maxThrust / totalMag);

                return total;
            }
        }

        /// <summary>
        /// Compute torque for orbit alignment. Blends between approach attitude (toward tangent point)
        /// and orbit attitude (forward=tangent, up=toward center) based on orbitBlendFactor.
        /// </summary>
        public static float3 ComputeOrbitAlignTorque(
            quaternion currentRotation, float3 shipPosition, float3 orbitCenter,
            float3 approachTarget, float orbitBlendFactor, float rotationTorque)
        {
            // Approach attitude: face toward approach target
            var toApproach = math.normalizesafe(approachTarget - shipPosition);
            if (math.lengthsq(toApproach) < 0.0001f)
                toApproach = math.forward(currentRotation);

            var approachRot = quaternion.LookRotationSafe(toApproach, math.up());

            // Orbit attitude: forward = tangent direction, up = toward center
            var toCenter = math.normalizesafe(orbitCenter - shipPosition);
            var tangent = math.cross(toCenter, math.up());
            if (math.lengthsq(tangent) < 0.0001f)
                tangent = math.cross(toCenter, math.right());
            tangent = math.normalize(tangent);

            var orbitRot = quaternion.LookRotationSafe(tangent, toCenter);

            // Blend desired rotation
            var desiredRot = math.slerp(approachRot, orbitRot, math.saturate(orbitBlendFactor));

            // Extract axis-angle error
            var errorQuat = math.mul(desiredRot, math.inverse(currentRotation));

            // Ensure shortest path
            if (errorQuat.value.w < 0f)
                errorQuat.value = -errorQuat.value;

            var errorAxis = new float3(errorQuat.value.x, errorQuat.value.y, errorQuat.value.z);
            float sinHalfAngle = math.length(errorAxis);

            if (sinHalfAngle < 0.001f)
                return float3.zero;

            float halfAngle = math.asin(math.clamp(sinHalfAngle, -1f, 1f));
            float angle = halfAngle * 2f;
            var axis = errorAxis / sinHalfAngle;

            return axis * angle * rotationTorque;
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
