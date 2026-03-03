using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Ship.Tests
{
    /// <summary>
    /// TDD tests for auto-pilot flight mode behaviors in ShipPhysicsMath.
    /// Covers DetermineFlightMode transitions and the four auto-pilot compute methods:
    /// ComputeAlignTorque, ComputeApproachThrust, ComputeOrbitThrust, ComputeKeepAtRangeThrust.
    /// RED phase: these tests will fail until the corresponding methods are implemented.
    /// </summary>
    [TestFixture]
    public class ShipFlightModeTests
    {
        private const float Epsilon = 0.001f;
        private const float MaxThrust = 1000f;
        private const float RotationTorque = 50f;
        private const float Mass = 1000f;
        private const float LinearDamping = 0.5f;
        private const float MaxSpeed = 300f;

        // ──────────────────────────────────────────────
        // DetermineFlightMode — auto-pilot transitions
        // ──────────────────────────────────────────────

        [Test]
        public void DetermineFlightMode_HasAlignPoint_NoManualInput_ReturnsAlignToPoint()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle,
                forward: 0f, strafe: 0f, roll: 0f,
                hasAlignPoint: true, radialAction: -1);

            Assert.AreEqual(ShipFlightMode.AlignToPoint, mode);
        }

        [Test]
        public void DetermineFlightMode_RadialAction0_ReturnsApproach()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle,
                forward: 0f, strafe: 0f, roll: 0f,
                hasAlignPoint: false, radialAction: 0);

            Assert.AreEqual(ShipFlightMode.Approach, mode);
        }

        [Test]
        public void DetermineFlightMode_RadialAction1_ReturnsOrbit()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle,
                forward: 0f, strafe: 0f, roll: 0f,
                hasAlignPoint: false, radialAction: 1);

            Assert.AreEqual(ShipFlightMode.Orbit, mode);
        }

        [Test]
        public void DetermineFlightMode_RadialAction3_ReturnsKeepAtRange()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle,
                forward: 0f, strafe: 0f, roll: 0f,
                hasAlignPoint: false, radialAction: 3);

            Assert.AreEqual(ShipFlightMode.KeepAtRange, mode);
        }

        [Test]
        public void DetermineFlightMode_RadialActionOverridesAlignPoint()
        {
            // Radial action takes priority over align point
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle,
                forward: 0f, strafe: 0f, roll: 0f,
                hasAlignPoint: true, radialAction: 0);

            Assert.AreEqual(ShipFlightMode.Approach, mode);
        }

        // ──────────────────────────────────────────────
        // ComputeAlignTorque
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeAlignTorque_TargetToRight_ProducesTorqueTowardTarget()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = math.normalize(new float3(1f, 0f, 1f));

            var torque = ShipPhysicsMath.ComputeAlignTorque(
                currentForward, toTarget, RotationTorque);

            // Cross product of forward x toTarget with +x component produces
            // torque around -y axis (yaw right), so torque.y should be negative
            // (or positive depending on convention). The key invariant:
            // torque magnitude should be non-zero and directed to rotate toward target.
            Assert.Greater(math.length(torque), Epsilon,
                "Torque magnitude should be non-zero when not aligned");

            // The cross product of (0,0,1) x normalize(1,0,1) has a -y component,
            // meaning the torque rotates the ship to face rightward toward the target.
            Assert.AreNotEqual(0f, torque.y, "Torque should have a yaw component");
        }

        [Test]
        public void ComputeAlignTorque_AlreadyAligned_ReturnsZero()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(0f, 0f, 1f);

            var torque = ShipPhysicsMath.ComputeAlignTorque(
                currentForward, toTarget, RotationTorque);

            Assert.AreEqual(0f, math.length(torque), Epsilon,
                "Torque should be zero when already facing target");
        }

        [Test]
        public void ComputeAlignTorque_TargetAbove_ProducesPitchTorque()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = math.normalize(new float3(0f, 1f, 1f));

            var torque = ShipPhysicsMath.ComputeAlignTorque(
                currentForward, toTarget, RotationTorque);

            // Cross of (0,0,1) x normalize(0,1,1) should produce torque around x-axis (pitch)
            Assert.Greater(math.length(torque), Epsilon,
                "Torque should be non-zero for pitch alignment");
            Assert.AreNotEqual(0f, torque.x, "Torque should have a pitch component");
        }

        // ──────────────────────────────────────────────
        // ComputeApproachThrust
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeApproachThrust_AlignedAndFar_AppliesForwardThrust()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(0f, 0f, 1f); // Perfectly aligned
            float distance = 500f;
            float targetDistance = 50f;

            var thrust = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            // Should produce forward thrust toward target
            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust when aligned and far from target");
            Assert.Greater(math.dot(thrust, toTarget), 0f,
                "Thrust should be directed toward target");
        }

        [Test]
        public void ComputeApproachThrust_CloseToTargetDistance_Decelerates()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(0f, 0f, 1f);
            float distance = 55f;        // Just barely beyond target distance
            float targetDistance = 50f;

            var thrustClose = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            var thrustFar = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, toTarget, 500f, targetDistance, MaxThrust);

            // Thrust when close should be less than thrust when far (deceleration)
            Assert.Less(math.length(thrustClose), math.length(thrustFar),
                "Thrust should decrease as ship approaches target distance");
        }

        [Test]
        public void ComputeApproachThrust_AtTargetDistance_ProducesZero()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(0f, 0f, 1f);
            float distance = 50f;
            float targetDistance = 50f;

            var thrust = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            Assert.AreEqual(0f, math.length(thrust), Epsilon,
                "Should produce zero thrust when at target distance");
        }

        [Test]
        public void ComputeApproachThrust_NotAligned_ReducesThrust()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = math.normalize(new float3(1f, 0f, 0f)); // 90 degrees off
            float distance = 500f;
            float targetDistance = 50f;

            var thrustMisaligned = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            var thrustAligned = ShipPhysicsMath.ComputeApproachThrust(
                currentForward, new float3(0f, 0f, 1f), distance, targetDistance, MaxThrust);

            // When misaligned, thrust should be significantly less than when aligned
            Assert.Less(math.length(thrustMisaligned), math.length(thrustAligned),
                "Thrust should be reduced when not aligned with target");
        }

        // ──────────────────────────────────────────────
        // ComputeMaxOrbitalSpeed
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeMaxOrbitalSpeed_ReturnsPositiveSpeed()
        {
            float speed = ShipPhysicsMath.ComputeMaxOrbitalSpeed(
                MaxThrust, Mass, LinearDamping, MaxSpeed, 100f);

            Assert.Greater(speed, 0f, "Should return positive speed for valid inputs");
        }

        [Test]
        public void ComputeMaxOrbitalSpeed_ClampsToMaxSpeed()
        {
            // Very high thrust, low damping, large radius → unconstrained speed would exceed maxSpeed
            float speed = ShipPhysicsMath.ComputeMaxOrbitalSpeed(
                100000f, 10f, 0.01f, 50f, 1000f);

            Assert.LessOrEqual(speed, 50f, "Should clamp to maxSpeed");
        }

        [Test]
        public void ComputeMaxOrbitalSpeed_ZeroRadius_ReturnsZero()
        {
            float speed = ShipPhysicsMath.ComputeMaxOrbitalSpeed(
                MaxThrust, Mass, LinearDamping, MaxSpeed, 0f);

            Assert.AreEqual(0f, speed, Epsilon, "Should return zero for zero radius");
        }

        [Test]
        public void ComputeMaxOrbitalSpeed_ZeroMass_ReturnsZero()
        {
            float speed = ShipPhysicsMath.ComputeMaxOrbitalSpeed(
                MaxThrust, 0f, LinearDamping, MaxSpeed, 100f);

            Assert.AreEqual(0f, speed, Epsilon, "Should return zero for zero mass");
        }

        [Test]
        public void ComputeMaxOrbitalSpeed_RealisticValues_ReasonableResult()
        {
            // Starter ship: thrust=5000, mass=1000, damping=0.5, maxSpeed=300, radius=250
            float speed = ShipPhysicsMath.ComputeMaxOrbitalSpeed(
                5000f, 1000f, 0.5f, 300f, 250f);

            Assert.Greater(speed, 1f, "Should produce meaningful speed");
            Assert.LessOrEqual(speed, 300f, "Should not exceed maxSpeed");
        }

        // ──────────────────────────────────────────────
        // ComputeOrbitTangentPoint
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeOrbitTangentPoint_OutsideCircle_ReturnsPointOnCircle()
        {
            var shipPos = new float3(200f, 0f, 0f);
            var center = new float3(0f, 0f, 0f);
            float radius = 100f;

            var tangent = ShipPhysicsMath.ComputeOrbitTangentPoint(shipPos, center, radius);
            float distFromCenter = math.length(tangent - center);

            Assert.AreEqual(radius, distFromCenter, 1f,
                "Tangent point should lie on the orbit circle");
        }

        [Test]
        public void ComputeOrbitTangentPoint_InsideCircle_ReturnsNearestEdgePoint()
        {
            var shipPos = new float3(30f, 0f, 0f);
            var center = new float3(0f, 0f, 0f);
            float radius = 100f;

            var tangent = ShipPhysicsMath.ComputeOrbitTangentPoint(shipPos, center, radius);
            float distFromCenter = math.length(new float3(tangent.x, 0f, tangent.z) - center);

            Assert.AreEqual(radius, distFromCenter, 1f,
                "When inside, should return point on circle edge");

            // Should be in the same direction as ship from center
            float dot = math.dot(
                math.normalize(new float3(shipPos.x, 0f, shipPos.z)),
                math.normalize(new float3(tangent.x, 0f, tangent.z)));
            Assert.Greater(dot, 0.9f, "Should project outward in same direction");
        }

        [Test]
        public void ComputeOrbitTangentPoint_OnCircle_ReturnsPointOnCircle()
        {
            var shipPos = new float3(100f, 0f, 0f);
            var center = new float3(0f, 0f, 0f);
            float radius = 100f;

            var tangent = ShipPhysicsMath.ComputeOrbitTangentPoint(shipPos, center, radius);
            float distFromCenter = math.length(new float3(tangent.x, 0f, tangent.z) - center);

            Assert.AreEqual(radius, distFromCenter, 1f,
                "When on circle, should return point on circle");
        }

        [Test]
        public void ComputeOrbitTangentPoint_VerticalAlignment_ReturnsValidPoint()
        {
            // Ship directly above center
            var shipPos = new float3(0f, 200f, 0f);
            var center = new float3(0f, 0f, 0f);
            float radius = 100f;

            var tangent = ShipPhysicsMath.ComputeOrbitTangentPoint(shipPos, center, radius);

            Assert.IsFalse(math.any(math.isnan(tangent)), "Should not produce NaN for vertical alignment");
            float distXZ = math.length(new float3(tangent.x, 0f, tangent.z) - center);
            Assert.AreEqual(radius, distXZ, 1f, "Should return valid point on circle");
        }

        // ──────────────────────────────────────────────
        // IsOnOrbitalCircle
        // ──────────────────────────────────────────────

        [Test]
        public void IsOnOrbitalCircle_AtRadius_ReturnsTrue()
        {
            Assert.IsTrue(ShipPhysicsMath.IsOnOrbitalCircle(100f, 100f),
                "Should return true when exactly at target distance");
        }

        [Test]
        public void IsOnOrbitalCircle_FarFromRadius_ReturnsFalse()
        {
            Assert.IsFalse(ShipPhysicsMath.IsOnOrbitalCircle(200f, 100f),
                "Should return false when far from target distance");
        }

        [Test]
        public void IsOnOrbitalCircle_WithinTolerance_ReturnsTrue()
        {
            // 10% of 100 = 10; distance is 108, within tolerance
            Assert.IsTrue(ShipPhysicsMath.IsOnOrbitalCircle(108f, 100f),
                "Should return true when within 10% tolerance");
        }

        [Test]
        public void IsOnOrbitalCircle_SmallRadius_Uses5mMinimum()
        {
            // 10% of 20 = 2, but minimum is 5; distance is 23, within 5m
            Assert.IsTrue(ShipPhysicsMath.IsOnOrbitalCircle(23f, 20f),
                "Should use 5m minimum threshold for small radii");
        }

        // ──────────────────────────────────────────────
        // ComputeOrbitThrust (new signature)
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeOrbitThrust_StableOrbit_ProducesTangentialThrust()
        {
            // Ship on orbit circle, target to the right
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var velocity = new float3(0f, 0f, 10f); // Moving tangentially
            float distance = 100f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust in stable orbit");
        }

        [Test]
        public void ComputeOrbitThrust_StableOrbit_HasCentripetalComponent()
        {
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var velocity = new float3(0f, 0f, 30f); // Tangential velocity
            float distance = 100f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            // toCenter = normalize(0 - (100,0,0)) = (-1,0,0)
            var toCenter = math.normalize(orbitCenter - shipPos);
            float centripetalComponent = math.dot(thrust, toCenter);
            Assert.Greater(centripetalComponent, 0f,
                "Stable orbit should have centripetal force toward center");
        }

        [Test]
        public void ComputeOrbitThrust_StableOrbit_PDCorrectionDampsRadialVelocity()
        {
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            // Ship at correct distance but drifting outward
            var velocityDrifting = new float3(20f, 0f, 10f); // +x = away from center
            var velocityStable = new float3(0f, 0f, 10f);    // No radial velocity

            float distance = 100f;
            float targetDistance = 100f;

            var thrustDrifting = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocityDrifting, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            var thrustStable = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocityStable, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            // Drifting case should have more inward force to counteract radial drift
            var toCenter = math.normalize(orbitCenter - shipPos);
            float inwardDrifting = math.dot(thrustDrifting, toCenter);
            float inwardStable = math.dot(thrustStable, toCenter);

            Assert.Greater(inwardDrifting, inwardStable,
                "PD correction should produce more inward force when drifting outward");
        }

        [Test]
        public void ComputeOrbitThrust_TooFar_AdjustsRadiallyInward()
        {
            var shipPos = new float3(200f, 0f, 0f);
            var orbitCenter = float3.zero;
            var velocity = float3.zero;
            float distance = 200f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            var toCenter = math.normalize(orbitCenter - shipPos);
            float radialComponent = math.dot(thrust, toCenter);
            Assert.Greater(radialComponent, 0f,
                "Should have inward thrust when too far from orbit distance");
        }

        [Test]
        public void ComputeOrbitThrust_TooClose_AdjustsRadiallyOutward()
        {
            var shipPos = new float3(30f, 0f, 0f);
            var orbitCenter = float3.zero;
            var velocity = float3.zero;
            float distance = 30f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            var toCenter = math.normalize(orbitCenter - shipPos);
            float radialComponent = math.dot(thrust, toCenter);
            Assert.Less(radialComponent, 0f,
                "Should have outward thrust when too close to orbit distance");
        }

        [Test]
        public void ComputeOrbitThrust_Approach_ThrustsTowardTangentPoint()
        {
            var shipPos = new float3(300f, 0f, 0f); // Well outside orbit
            var orbitCenter = float3.zero;
            var velocity = float3.zero;
            float distance = 300f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust during approach phase");

            // Thrust should have an inward component (toward center)
            var toCenter = math.normalize(orbitCenter - shipPos);
            float inwardComponent = math.dot(thrust, toCenter);
            Assert.Greater(inwardComponent, 0f,
                "Approach thrust should have inward component toward orbit");
        }

        [Test]
        public void ComputeOrbitThrust_ZeroDistance_ReturnsZero()
        {
            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                float3.zero, float3.zero, float3.zero, 0f,
                100f, MaxThrust, Mass, LinearDamping, MaxSpeed);

            Assert.AreEqual(0f, math.length(thrust), Epsilon,
                "Should return zero thrust at zero distance");
        }

        [Test]
        public void ComputeOrbitThrust_ClampedToMaxThrust()
        {
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var velocity = new float3(0f, 0f, 200f); // High speed
            float distance = 100f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                shipPos, orbitCenter, velocity, distance,
                targetDistance, MaxThrust, Mass, LinearDamping, MaxSpeed);

            Assert.LessOrEqual(math.length(thrust), MaxThrust + Epsilon,
                "Total thrust should not exceed maxThrust");
        }

        // ──────────────────────────────────────────────
        // ComputeOrbitAlignTorque
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeOrbitAlignTorque_ApproachPhase_FacesTarget()
        {
            var currentRot = quaternion.identity; // Facing +Z
            var shipPos = new float3(300f, 0f, 0f);
            var orbitCenter = float3.zero;
            var approachTarget = new float3(100f, 0f, 50f);

            var torque = ShipPhysicsMath.ComputeOrbitAlignTorque(
                currentRot, shipPos, orbitCenter, approachTarget, 0f, RotationTorque);

            Assert.Greater(math.length(torque), Epsilon,
                "Should produce torque to rotate toward approach target");
        }

        [Test]
        public void ComputeOrbitAlignTorque_OrbitPhase_FacesTangent()
        {
            var currentRot = quaternion.identity; // Facing +Z
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var approachTarget = new float3(100f, 0f, 50f); // irrelevant at blend=1

            var torque = ShipPhysicsMath.ComputeOrbitAlignTorque(
                currentRot, shipPos, orbitCenter, approachTarget, 1f, RotationTorque);

            // At blend=1, desired forward = tangent direction (which is +Z or -Z for ship at +X)
            // Current forward is +Z, tangent is in Z direction, so torque may be small but defined
            Assert.IsFalse(math.any(math.isnan(torque)), "Should not produce NaN torque");
        }

        [Test]
        public void ComputeOrbitAlignTorque_BlendInterpolation_MidBlendDiffers()
        {
            var currentRot = quaternion.LookRotationSafe(new float3(1f, 0f, 0f), math.up());
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var approachTarget = new float3(50f, 0f, 100f);

            var torqueApproach = ShipPhysicsMath.ComputeOrbitAlignTorque(
                currentRot, shipPos, orbitCenter, approachTarget, 0f, RotationTorque);
            var torqueMid = ShipPhysicsMath.ComputeOrbitAlignTorque(
                currentRot, shipPos, orbitCenter, approachTarget, 0.5f, RotationTorque);
            var torqueOrbit = ShipPhysicsMath.ComputeOrbitAlignTorque(
                currentRot, shipPos, orbitCenter, approachTarget, 1f, RotationTorque);

            // Mid-blend should differ from both extremes (unless they happen to be identical)
            float magApproach = math.length(torqueApproach);
            float magMid = math.length(torqueMid);
            float magOrbit = math.length(torqueOrbit);

            // At least one pair should differ
            bool differs = math.abs(magApproach - magMid) > Epsilon ||
                           math.abs(magMid - magOrbit) > Epsilon;
            Assert.IsTrue(differs, "Mid-blend torque should differ from at least one extreme");
        }

        [Test]
        public void ComputeOrbitAlignTorque_AlreadyAligned_ReturnsNearZero()
        {
            // Ship at +X facing tangent direction (-Z), with up toward center (-X)
            var shipPos = new float3(100f, 0f, 0f);
            var orbitCenter = float3.zero;
            var toCenter = math.normalize(orbitCenter - shipPos); // (-1,0,0)
            var tangent = math.normalize(math.cross(toCenter, math.up())); // should be (0,0,-1) or (0,0,1)

            var alignedRot = quaternion.LookRotationSafe(tangent, toCenter);

            var torque = ShipPhysicsMath.ComputeOrbitAlignTorque(
                alignedRot, shipPos, orbitCenter, shipPos + tangent * 50f, 1f, RotationTorque);

            Assert.Less(math.length(torque), RotationTorque * 0.1f,
                "Torque should be near zero when already in correct orbit attitude");
        }

        // ──────────────────────────────────────────────
        // Multi-frame integration: orbit stability
        // ──────────────────────────────────────────────

        [Test]
        public void OrbitIntegration_100Frames_MaintainsDistance()
        {
            // Simulate a ship already on the orbit circle for 100 frames
            float targetDistance = 250f;
            float mass = 1000f;
            float maxThrust = 5000f;
            float linearDamping = 0.5f;
            float maxSpd = 300f;
            float dt = 0.016f;
            var orbitCenter = float3.zero;

            // Start on the orbit circle with some tangential velocity
            var pos = new float3(targetDistance, 0f, 0f);
            var vel = new float3(0f, 0f, 10f); // Initial tangential kick

            // Converge for 200 warm-up frames
            for (int i = 0; i < 200; i++)
            {
                float dist = math.length(pos - orbitCenter);
                var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                    pos, orbitCenter, vel, dist,
                    targetDistance, maxThrust, mass, linearDamping, maxSpd);

                vel = ShipPhysicsMath.ApplyForce(vel, thrust, mass, dt);
                vel = ShipPhysicsMath.ApplyDamping(vel, linearDamping, dt);
                vel = ShipPhysicsMath.ClampSpeed(vel, maxSpd);
                pos += vel * dt;
            }

            // Now measure stability over 100 frames
            float minDist = float.MaxValue;
            float maxDist = float.MinValue;

            for (int i = 0; i < 100; i++)
            {
                float dist = math.length(pos - orbitCenter);
                minDist = math.min(minDist, dist);
                maxDist = math.max(maxDist, dist);

                var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                    pos, orbitCenter, vel, dist,
                    targetDistance, maxThrust, mass, linearDamping, maxSpd);

                vel = ShipPhysicsMath.ApplyForce(vel, thrust, mass, dt);
                vel = ShipPhysicsMath.ApplyDamping(vel, linearDamping, dt);
                vel = ShipPhysicsMath.ClampSpeed(vel, maxSpd);
                pos += vel * dt;
            }

            float tolerance = targetDistance * 0.05f; // 5% tolerance (generous for convergence)
            Assert.AreEqual(targetDistance, (minDist + maxDist) / 2f, tolerance,
                $"Average distance should be near target. Min={minDist:F1}, Max={maxDist:F1}, Target={targetDistance}");
        }

        // ──────────────────────────────────────────────
        // ComputeKeepAtRangeThrust
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeKeepAtRangeThrust_TooFar_MovesTowardTarget()
        {
            var toTarget = new float3(1f, 0f, 0f);
            float distance = 200f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeKeepAtRangeThrust(
                toTarget, distance, targetDistance, MaxThrust);

            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust when too far");
            Assert.Greater(math.dot(thrust, toTarget), 0f,
                "Thrust should be directed toward target when too far");
        }

        [Test]
        public void ComputeKeepAtRangeThrust_TooClose_MovesAwayFromTarget()
        {
            var toTarget = new float3(1f, 0f, 0f);
            float distance = 50f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeKeepAtRangeThrust(
                toTarget, distance, targetDistance, MaxThrust);

            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust when too close");
            Assert.Less(math.dot(thrust, toTarget), 0f,
                "Thrust should be directed away from target when too close");
        }

        [Test]
        public void ComputeKeepAtRangeThrust_AtTargetDistance_ProducesZero()
        {
            var toTarget = new float3(1f, 0f, 0f);
            float distance = 100f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeKeepAtRangeThrust(
                toTarget, distance, targetDistance, MaxThrust);

            Assert.AreEqual(0f, math.length(thrust), Epsilon,
                "Should produce zero thrust when at target distance");
        }

        [Test]
        public void ComputeKeepAtRangeThrust_OnlyRadialNoLateral()
        {
            var toTarget = math.normalize(new float3(1f, 0f, 1f));
            float distance = 200f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeKeepAtRangeThrust(
                toTarget, distance, targetDistance, MaxThrust);

            // Thrust should be purely along the toTarget axis (no lateral component)
            if (math.length(thrust) > Epsilon)
            {
                var thrustDir = math.normalize(thrust);
                float alignment = math.abs(math.dot(thrustDir, toTarget));
                Assert.AreEqual(1f, alignment, 0.01f,
                    "KeepAtRange thrust should be purely radial (along toTarget axis)");
            }
        }
    }
}
