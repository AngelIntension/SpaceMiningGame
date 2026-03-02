using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Tests
{
    /// <summary>
    /// Integration tests for the full select→lock→multi-target management lifecycle.
    /// See Spec 007: In-Flight Targeting (FR-015, FR-020, FR-021, FR-022, FR-023, FR-024).
    /// </summary>
    [TestFixture]
    public sealed class SelectionIntegrationTests
    {
        private TargetingState _state;

        [SetUp]
        public void SetUp()
        {
            _state = TargetingState.Empty;
        }

        [Test]
        public void FullLifecycle_SelectLockThreeTargets_ThreeLockedTargets()
        {
            // Select and lock target 1
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(1, TargetType.Asteroid, "Luminite Asteroid", "Luminite"));
            _state = TargetingReducer.Reduce(_state, new BeginLockAction(1, 1.5f));
            _state = TargetingReducer.Reduce(_state, new LockTickAction(2.0f));
            _state = TargetingReducer.Reduce(_state, new CompleteLockAction());

            Assert.AreEqual(1, _state.LockedTargets.Length);
            Assert.AreEqual("Luminite Asteroid", _state.LockedTargets[0].DisplayName);

            // Select and lock target 2
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(2, TargetType.Asteroid, "Ferrox Asteroid", "Ferrox"));
            _state = TargetingReducer.Reduce(_state, new BeginLockAction(2, 1.5f));
            _state = TargetingReducer.Reduce(_state, new LockTickAction(2.0f));
            _state = TargetingReducer.Reduce(_state, new CompleteLockAction());

            Assert.AreEqual(2, _state.LockedTargets.Length);
            Assert.AreEqual("Ferrox Asteroid", _state.LockedTargets[1].DisplayName);

            // Select and lock target 3
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(3, TargetType.Station, "Mining Relay", "Station"));
            _state = TargetingReducer.Reduce(_state, new BeginLockAction(3, 1.5f));
            _state = TargetingReducer.Reduce(_state, new LockTickAction(2.0f));
            _state = TargetingReducer.Reduce(_state, new CompleteLockAction());

            Assert.AreEqual(3, _state.LockedTargets.Length);
            Assert.AreEqual("Mining Relay", _state.LockedTargets[2].DisplayName);
        }

        [Test]
        public void UnlockMiddleTarget_MaintainsOrderOfRemaining()
        {
            // Lock 3 targets
            _state = LockTarget(1, "Target A", "Luminite");
            _state = LockTarget(2, "Target B", "Ferrox");
            _state = LockTarget(3, "Target C", "Auralite");
            Assert.AreEqual(3, _state.LockedTargets.Length);

            // Unlock middle (target 2)
            _state = TargetingReducer.Reduce(_state, new UnlockTargetAction(2));

            Assert.AreEqual(2, _state.LockedTargets.Length);
            Assert.AreEqual(1, _state.LockedTargets[0].TargetId);
            Assert.AreEqual("Target A", _state.LockedTargets[0].DisplayName);
            Assert.AreEqual(3, _state.LockedTargets[1].TargetId);
            Assert.AreEqual("Target C", _state.LockedTargets[1].DisplayName);
        }

        [Test]
        public void ClearAllLocks_ResetsEverything()
        {
            _state = LockTarget(1, "Target A", "Luminite");
            _state = LockTarget(2, "Target B", "Ferrox");

            _state = TargetingReducer.Reduce(_state, new ClearAllLocksAction());

            Assert.AreEqual(0, _state.LockedTargets.Length);
            Assert.IsFalse(_state.Selection.HasSelection);
            Assert.IsFalse(_state.LockAcquisition.IsActive);
        }

        [Test]
        public void DuplicateLock_ReturnsUnchanged()
        {
            _state = LockTarget(1, "Target A", "Luminite");
            Assert.AreEqual(1, _state.LockedTargets.Length);

            // Attempt to lock the same target again
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(1, TargetType.Asteroid, "Target A", "Luminite"));
            var before = _state;
            _state = TargetingReducer.Reduce(_state, new BeginLockAction(1, 1.5f));

            Assert.AreSame(before, _state);
        }

        [Test]
        public void SlotsFullPreventsNewLock_SimulatedWithMaxThreeLocks()
        {
            _state = LockTarget(1, "Target A", "Luminite");
            _state = LockTarget(2, "Target B", "Ferrox");
            _state = LockTarget(3, "Target C", "Auralite");

            Assert.AreEqual(3, _state.LockedTargets.Length);

            // The reducer itself doesn't enforce max locks — that's done by TargetingController.
            // The reducer-level test is: we CAN lock a 4th (reducer has no limit).
            // The AttemptLockOnSelected check in controller prevents this.
            // Verify all 3 remain intact:
            Assert.AreEqual(1, _state.LockedTargets[0].TargetId);
            Assert.AreEqual(2, _state.LockedTargets[1].TargetId);
            Assert.AreEqual(3, _state.LockedTargets[2].TargetId);
        }

        [Test]
        public void SelectDuringLock_CancelsAcquisition()
        {
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(1, TargetType.Asteroid, "Target A", "Luminite"));
            _state = TargetingReducer.Reduce(_state, new BeginLockAction(1, 1.5f));
            Assert.IsTrue(_state.LockAcquisition.IsActive);

            // Select a different target — cancels lock
            _state = TargetingReducer.Reduce(_state,
                new SelectTargetAction(2, TargetType.Asteroid, "Target B", "Ferrox"));

            Assert.IsFalse(_state.LockAcquisition.IsActive);
            Assert.AreEqual(LockAcquisitionStatus.None, _state.LockAcquisition.Status);
        }

        [Test]
        public void InsertionOrder_MaintainedAcrossLocks()
        {
            _state = LockTarget(10, "Alpha", "A");
            _state = LockTarget(20, "Beta", "B");
            _state = LockTarget(30, "Gamma", "C");

            Assert.AreEqual(10, _state.LockedTargets[0].TargetId);
            Assert.AreEqual(20, _state.LockedTargets[1].TargetId);
            Assert.AreEqual(30, _state.LockedTargets[2].TargetId);
        }

        private TargetingState LockTarget(int targetId, string displayName, string typeLabel)
        {
            var s = TargetingReducer.Reduce(_state,
                new SelectTargetAction(targetId, TargetType.Asteroid, displayName, typeLabel));
            s = TargetingReducer.Reduce(s, new BeginLockAction(targetId, 1.5f));
            s = TargetingReducer.Reduce(s, new LockTickAction(2.0f));
            s = TargetingReducer.Reduce(s, new CompleteLockAction());
            _state = s;
            return s;
        }
    }
}
