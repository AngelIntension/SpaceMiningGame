using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Docking.Systems;

namespace VoidHarvest.Features.Docking.Tests
{
    [TestFixture]
    public class DockingReducerTests
    {
        private static readonly float3 TestPortPos = new(100f, 0f, 0f);
        private static readonly quaternion TestPortRot = quaternion.identity;
        private const int TestStationId = 1;

        [Test]
        public void None_BeginDocking_TransitionsToApproaching()
        {
            var state = DockingState.Empty;
            var action = new BeginDockingAction(TestStationId, TestPortPos, TestPortRot);

            var result = DockingReducer.Reduce(state, action);

            Assert.AreEqual(DockingPhase.Approaching, result.Phase);
            Assert.IsTrue(result.TargetStationId.HasValue);
            Assert.AreEqual(TestStationId, result.TargetStationId.GetValueOrDefault(-1));
            Assert.IsTrue(result.DockingPortPosition.HasValue);
            Assert.IsTrue(result.DockingPortRotation.HasValue);
        }

        [Test]
        public void Approaching_CompleteDocking_TransitionsToDocked()
        {
            var state = new DockingState(
                DockingPhase.Approaching,
                TestStationId,
                TestPortPos,
                TestPortRot
            );
            var action = new CompleteDockingAction(TestStationId);

            var result = DockingReducer.Reduce(state, action);

            Assert.AreEqual(DockingPhase.Docked, result.Phase);
            Assert.IsTrue(result.IsDocked);
        }

        [Test]
        public void Approaching_Cancel_TransitionsToNone()
        {
            var state = new DockingState(
                DockingPhase.Approaching,
                TestStationId,
                TestPortPos,
                TestPortRot
            );
            var action = new CancelDockingAction();

            var result = DockingReducer.Reduce(state, action);

            Assert.AreEqual(DockingPhase.None, result.Phase);
            Assert.IsFalse(result.TargetStationId.HasValue);
            Assert.IsFalse(result.DockingPortPosition.HasValue);
            Assert.IsFalse(result.DockingPortRotation.HasValue);
        }

        [Test]
        public void Docked_BeginUndocking_TransitionsToUndocking()
        {
            var state = new DockingState(
                DockingPhase.Docked,
                TestStationId,
                TestPortPos,
                TestPortRot
            );
            var action = new BeginUndockingAction();

            var result = DockingReducer.Reduce(state, action);

            Assert.AreEqual(DockingPhase.Undocking, result.Phase);
        }

        [Test]
        public void Undocking_CompleteUndocking_TransitionsToNone()
        {
            var state = new DockingState(
                DockingPhase.Undocking,
                TestStationId,
                TestPortPos,
                TestPortRot
            );
            var action = new CompleteUndockingAction();

            var result = DockingReducer.Reduce(state, action);

            Assert.AreEqual(DockingPhase.None, result.Phase);
            Assert.IsFalse(result.TargetStationId.HasValue);
        }

        [Test]
        public void InvalidTransition_None_CompleteDocking_ReturnsUnchanged()
        {
            var state = DockingState.Empty;
            var action = new CompleteDockingAction(TestStationId);

            var result = DockingReducer.Reduce(state, action);

            Assert.AreSame(state, result);
        }

        [Test]
        public void InvalidTransition_None_BeginUndocking_ReturnsUnchanged()
        {
            var state = DockingState.Empty;
            var action = new BeginUndockingAction();

            var result = DockingReducer.Reduce(state, action);

            Assert.AreSame(state, result);
        }

        [Test]
        public void InvalidTransition_Docked_BeginDocking_ReturnsUnchanged()
        {
            var state = new DockingState(
                DockingPhase.Docked,
                TestStationId,
                TestPortPos,
                TestPortRot
            );
            var action = new BeginDockingAction(2, new float3(200f, 0f, 0f), quaternion.identity);

            var result = DockingReducer.Reduce(state, action);

            Assert.AreSame(state, result);
        }

        [Test]
        public void BeginDocking_PropagatesFieldValues()
        {
            var portPos = new float3(42f, 13f, 7f);
            var portRot = quaternion.AxisAngle(new float3(0, 1, 0), math.radians(90f));
            var action = new BeginDockingAction(99, portPos, portRot);

            var result = DockingReducer.Reduce(DockingState.Empty, action);

            Assert.AreEqual(99, result.TargetStationId.GetValueOrDefault(-1));
            var pos = result.DockingPortPosition.GetValueOrDefault(float3.zero);
            Assert.AreEqual(42f, pos.x, 0.001f);
            Assert.AreEqual(13f, pos.y, 0.001f);
            Assert.AreEqual(7f, pos.z, 0.001f);
        }

        [Test]
        public void DockingState_IsDocked_TrueOnlyWhenDocked()
        {
            Assert.IsFalse(DockingState.Empty.IsDocked);
            Assert.IsFalse(new DockingState(DockingPhase.Approaching, default, default, default).IsDocked);
            Assert.IsTrue(new DockingState(DockingPhase.Docked, default, default, default).IsDocked);
            Assert.IsFalse(new DockingState(DockingPhase.Undocking, default, default, default).IsDocked);
        }

        [Test]
        public void DockingState_IsInProgress_TrueForApproachingAndSnapping()
        {
            Assert.IsFalse(DockingState.Empty.IsInProgress);
            Assert.IsTrue(new DockingState(DockingPhase.Approaching, default, default, default).IsInProgress);
            Assert.IsTrue(new DockingState(DockingPhase.Snapping, default, default, default).IsInProgress);
            Assert.IsFalse(new DockingState(DockingPhase.Docked, default, default, default).IsInProgress);
        }
    }
}
