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
        // ComputeOrbitThrust
        // ──────────────────────────────────────────────

        [Test]
        public void ComputeOrbitThrust_AtTargetDistance_ProducesLateralThrust()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(1f, 0f, 0f); // Target is to the right
            float distance = 100f;
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            Assert.Greater(math.length(thrust), Epsilon,
                "Should produce thrust to orbit even when at correct distance");

            // Lateral thrust should be perpendicular to the toTarget vector
            float dotWithTarget = math.abs(math.dot(math.normalize(thrust), toTarget));
            Assert.Less(dotWithTarget, 0.5f,
                "Orbit thrust should be primarily perpendicular to target direction");
        }

        [Test]
        public void ComputeOrbitThrust_TooFar_AdjustsRadiallyInward()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(1f, 0f, 0f);
            float distance = 200f;         // Much further than target
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            // Should have a radial component toward target to close distance
            float radialComponent = math.dot(thrust, toTarget);
            Assert.Greater(radialComponent, 0f,
                "Should have inward radial thrust when too far from orbit distance");
        }

        [Test]
        public void ComputeOrbitThrust_TooClose_AdjustsRadiallyOutward()
        {
            var currentForward = new float3(0f, 0f, 1f);
            var toTarget = new float3(1f, 0f, 0f);
            float distance = 50f;          // Closer than target
            float targetDistance = 100f;

            var thrust = ShipPhysicsMath.ComputeOrbitThrust(
                currentForward, toTarget, distance, targetDistance, MaxThrust);

            // Should have a radial component away from target to increase distance
            float radialComponent = math.dot(thrust, toTarget);
            Assert.Less(radialComponent, 0f,
                "Should have outward radial thrust when too close to orbit distance");
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
