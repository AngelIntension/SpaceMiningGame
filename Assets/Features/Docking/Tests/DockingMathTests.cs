using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Features.Docking.Systems;

namespace VoidHarvest.Features.Docking.Tests
{
    [TestFixture]
    public class DockingMathTests
    {
        [Test]
        public void IsWithinDockingRange_InsideRange_ReturnsTrue()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(100, 0, 0);
            Assert.IsTrue(DockingMath.IsWithinDockingRange(shipPos, portPos, 500f));
        }

        [Test]
        public void IsWithinDockingRange_OutsideRange_ReturnsFalse()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(600, 0, 0);
            Assert.IsFalse(DockingMath.IsWithinDockingRange(shipPos, portPos, 500f));
        }

        [Test]
        public void IsWithinDockingRange_ExactBoundary_ReturnsTrue()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(500, 0, 0);
            Assert.IsTrue(DockingMath.IsWithinDockingRange(shipPos, portPos, 500f));
        }

        [Test]
        public void IsWithinSnapRange_InsideRange_ReturnsTrue()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(20, 0, 0);
            Assert.IsTrue(DockingMath.IsWithinSnapRange(shipPos, portPos, 30f));
        }

        [Test]
        public void IsWithinSnapRange_OutsideRange_ReturnsFalse()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(50, 0, 0);
            Assert.IsFalse(DockingMath.IsWithinSnapRange(shipPos, portPos, 30f));
        }

        [Test]
        public void ComputeSnapProgress_AtZero_ReturnsZero()
        {
            Assert.AreEqual(0f, DockingMath.ComputeSnapProgress(0f, 1.5f), 0.001f);
        }

        [Test]
        public void ComputeSnapProgress_AtEnd_ReturnsOne()
        {
            Assert.AreEqual(1f, DockingMath.ComputeSnapProgress(1.5f, 1.5f), 0.001f);
        }

        [Test]
        public void ComputeSnapProgress_AtMidpoint_ReturnsSmoothstepHalf()
        {
            float t = DockingMath.ComputeSnapProgress(0.75f, 1.5f);
            Assert.AreEqual(0.5f, t, 0.001f);
        }

        [Test]
        public void ComputeSnapProgress_BeyondDuration_ClampsToOne()
        {
            Assert.AreEqual(1f, DockingMath.ComputeSnapProgress(3f, 1.5f), 0.001f);
        }

        [Test]
        public void InterpolateSnapPose_AtStart_ReturnsStartPose()
        {
            var startPos = new float3(0, 0, 0);
            var startRot = quaternion.identity;
            var targetPos = new float3(100, 0, 0);
            var targetRot = quaternion.AxisAngle(new float3(0, 1, 0), math.radians(90f));

            var (pos, rot) = DockingMath.InterpolateSnapPose(startPos, startRot, targetPos, targetRot, 0f);

            Assert.AreEqual(0f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
            Assert.AreEqual(0f, pos.z, 0.001f);
        }

        [Test]
        public void InterpolateSnapPose_AtEnd_ReturnsTargetPose()
        {
            var startPos = new float3(0, 0, 0);
            var startRot = quaternion.identity;
            var targetPos = new float3(100, 0, 0);
            var targetRot = quaternion.AxisAngle(new float3(0, 1, 0), math.radians(90f));

            var (pos, rot) = DockingMath.InterpolateSnapPose(startPos, startRot, targetPos, targetRot, 1f);

            Assert.AreEqual(100f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
            Assert.AreEqual(0f, pos.z, 0.001f);
        }

        [Test]
        public void InterpolateSnapPose_AtMidpoint_ReturnsInterpolated()
        {
            var startPos = new float3(0, 0, 0);
            var startRot = quaternion.identity;
            var targetPos = new float3(100, 0, 0);
            var targetRot = quaternion.identity;

            var (pos, _) = DockingMath.InterpolateSnapPose(startPos, startRot, targetPos, targetRot, 0.5f);

            Assert.AreEqual(50f, pos.x, 0.001f);
        }

        [Test]
        public void ComputeApproachTarget_ReturnsOffsetAlongDirection()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(100, 0, 0);

            var target = DockingMath.ComputeApproachTarget(shipPos, portPos, 50f);

            // Should be 50 units from the port, along the ship→port direction
            Assert.AreEqual(50f, target.x, 0.5f);
        }

        [Test]
        public void ComputeClearancePosition_ReturnsPositionAlongForward()
        {
            var portPos = new float3(0, 0, 0);
            var portForward = new float3(0, 0, 1);

            var clearance = DockingMath.ComputeClearancePosition(portPos, portForward, 100f);

            Assert.AreEqual(0f, clearance.x, 0.001f);
            Assert.AreEqual(0f, clearance.y, 0.001f);
            Assert.AreEqual(100f, clearance.z, 0.001f);
        }
    }
}
