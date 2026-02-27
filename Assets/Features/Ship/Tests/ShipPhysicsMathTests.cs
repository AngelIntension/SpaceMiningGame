using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Ship.Tests
{
    [TestFixture]
    public class ShipPhysicsMathTests
    {
        // ComputeThrust tests
        [Test]
        public void ComputeThrust_ForwardInput_ProducesForceAlongLocalZ()
        {
            var force = ShipPhysicsMath.ComputeThrust(
                math.forward(), math.right(), math.up(),
                1f, 0f, 0f,
                1000f, ShipFlightMode.ManualThrust);

            Assert.Greater(force.z, 0f);
            Assert.AreEqual(0f, force.x, 0.001f);
        }

        [Test]
        public void ComputeThrust_StrafeInput_ProducesForceAlongLocalX()
        {
            var force = ShipPhysicsMath.ComputeThrust(
                math.forward(), math.right(), math.up(),
                0f, 1f, 0f,
                1000f, ShipFlightMode.ManualThrust);

            Assert.Greater(force.x, 0f);
            Assert.AreEqual(0f, force.z, 0.001f);
        }

        [Test]
        public void ComputeThrust_NonManualMode_ReturnsZero()
        {
            var force = ShipPhysicsMath.ComputeThrust(
                math.forward(), math.right(), math.up(),
                1f, 1f, 1f,
                1000f, ShipFlightMode.Approach);

            Assert.AreEqual(float3.zero, force);
        }

        // ApplyForce tests
        [Test]
        public void ApplyForce_IncreasesVelocity()
        {
            var v = ShipPhysicsMath.ApplyForce(float3.zero, new float3(0, 0, 100f), 10f, 1f);
            Assert.AreEqual(10f, v.z, 0.001f); // F/m * dt = 100/10 * 1 = 10
        }

        [Test]
        public void ApplyForce_ZeroMass_ReturnsUnchanged()
        {
            var v = ShipPhysicsMath.ApplyForce(new float3(5, 0, 0), new float3(0, 0, 100f), 0f, 1f);
            Assert.AreEqual(5f, v.x, 0.001f);
        }

        // ApplyDamping tests
        [Test]
        public void ApplyDamping_ReducesVelocity()
        {
            var v = new float3(10f, 0, 0);
            var damped = ShipPhysicsMath.ApplyDamping(v, 0.5f, 1f);
            Assert.Less(math.length(damped), math.length(v));
        }

        [Test]
        public void ApplyDamping_NeverGoesNegative()
        {
            var v = new float3(1f, 0, 0);
            var damped = ShipPhysicsMath.ApplyDamping(v, 100f, 1f);
            Assert.GreaterOrEqual(math.length(damped), 0f);
        }

        // ClampSpeed tests
        [Test]
        public void ClampSpeed_BelowMax_Unchanged()
        {
            var v = new float3(5f, 0, 0);
            var clamped = ShipPhysicsMath.ClampSpeed(v, 100f);
            Assert.AreEqual(5f, math.length(clamped), 0.001f);
        }

        [Test]
        public void ClampSpeed_AboveMax_ClampedToMax()
        {
            var v = new float3(150f, 0, 0);
            var clamped = ShipPhysicsMath.ClampSpeed(v, 100f);
            Assert.AreEqual(100f, math.length(clamped), 0.001f);
        }

        // IntegrateRotation tests
        [Test]
        public void IntegrateRotation_ZeroAngularVelocity_Unchanged()
        {
            var q = quaternion.identity;
            var result = ShipPhysicsMath.IntegrateRotation(q, float3.zero, 1f);
            Assert.AreEqual(q.value.x, result.value.x, 0.001f);
            Assert.AreEqual(q.value.y, result.value.y, 0.001f);
            Assert.AreEqual(q.value.z, result.value.z, 0.001f);
            Assert.AreEqual(q.value.w, result.value.w, 0.001f);
        }

        [Test]
        public void IntegrateRotation_WithAngularVelocity_ProducesNormalizedResult()
        {
            var q = quaternion.identity;
            var result = ShipPhysicsMath.IntegrateRotation(q, new float3(0, 1f, 0), 0.1f);
            var len = math.length(result.value);
            Assert.AreEqual(1f, len, 0.01f);
        }

        // DetermineFlightMode tests
        [Test]
        public void DetermineFlightMode_ManualInput_ReturnsManualThrust()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle, 0.5f, 0f, 0f, false, -1);
            Assert.AreEqual(ShipFlightMode.ManualThrust, mode);
        }

        [Test]
        public void DetermineFlightMode_NoInput_ManualThrust_DecaysToIdle()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.ManualThrust, 0f, 0f, 0f, false, -1);
            Assert.AreEqual(ShipFlightMode.Idle, mode);
        }

        [Test]
        public void DetermineFlightMode_AlignPoint_ReturnsAlignToPoint()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Idle, 0f, 0f, 0f, true, -1);
            Assert.AreEqual(ShipFlightMode.AlignToPoint, mode);
        }

        [Test]
        public void DetermineFlightMode_ManualOverridesAutoPilot()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Approach, 1f, 0f, 0f, false, -1);
            Assert.AreEqual(ShipFlightMode.ManualThrust, mode);
        }

        // SanitizeVelocity tests
        [Test]
        public void SanitizeVelocity_NaN_ReturnsZero()
        {
            var v = new float3(float.NaN, 0, 0);
            var result = ShipPhysicsMath.SanitizeVelocity(v);
            Assert.AreEqual(float3.zero, result);
        }

        [Test]
        public void SanitizeVelocity_ValidInput_Unchanged()
        {
            var v = new float3(1, 2, 3);
            var result = ShipPhysicsMath.SanitizeVelocity(v);
            Assert.AreEqual(v, result);
        }
    }
}
