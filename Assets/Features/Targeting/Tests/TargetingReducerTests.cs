using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;
using VoidHarvest.Features.Targeting.Systems;

namespace VoidHarvest.Features.Targeting.Tests
{
    [TestFixture]
    public class TargetingReducerTests
    {
        private static readonly TargetingState EmptyState = TargetingState.Empty;

        // --- SelectTargetAction ---

        [Test]
        public void SelectTarget_SetsSelectionFields()
        {
            var action = new SelectTargetAction(42, TargetType.Asteroid, "Rock", "Luminite");
            var result = TargetingReducer.Reduce(EmptyState, action);

            Assert.AreEqual(42, result.Selection.TargetId);
            Assert.AreEqual(TargetType.Asteroid, result.Selection.TargetType);
            Assert.AreEqual("Rock", result.Selection.DisplayName);
            Assert.AreEqual("Luminite", result.Selection.TypeLabel);
            Assert.IsTrue(result.Selection.HasSelection);
        }

        [Test]
        public void SelectTarget_CancelsActiveLockIfTargetChanged()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(10, TargetType.Asteroid, "Old", "Old"),
                LockAcquisition = new LockAcquisitionData(10, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var action = new SelectTargetAction(20, TargetType.Station, "New", "Station");
            var result = TargetingReducer.Reduce(state, action);

            Assert.AreEqual(20, result.Selection.TargetId);
            Assert.AreEqual(LockAcquisitionStatus.None, result.LockAcquisition.Status);
            Assert.AreEqual(-1, result.LockAcquisition.TargetId);
        }

        [Test]
        public void SelectTarget_KeepsLockIfSameTarget()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(10, TargetType.Asteroid, "Rock", "Luminite"),
                LockAcquisition = new LockAcquisitionData(10, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var action = new SelectTargetAction(10, TargetType.Asteroid, "Rock", "Luminite");
            var result = TargetingReducer.Reduce(state, action);

            Assert.AreEqual(LockAcquisitionStatus.InProgress, result.LockAcquisition.Status);
            Assert.AreEqual(10, result.LockAcquisition.TargetId);
        }

        // --- ClearSelectionAction ---

        [Test]
        public void ClearSelection_ResetsSelectionToNone()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(42, TargetType.Asteroid, "Rock", "Luminite")
            };

            var result = TargetingReducer.Reduce(state, new ClearSelectionAction());

            Assert.IsFalse(result.Selection.HasSelection);
            Assert.AreEqual(-1, result.Selection.TargetId);
        }

        [Test]
        public void ClearSelection_CancelsActiveLock()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(10, TargetType.Asteroid, "Rock", "Luminite"),
                LockAcquisition = new LockAcquisitionData(10, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var result = TargetingReducer.Reduce(state, new ClearSelectionAction());

            Assert.AreEqual(LockAcquisitionStatus.None, result.LockAcquisition.Status);
        }

        // --- BeginLockAction ---

        [Test]
        public void BeginLock_SetsLockAcquisitionInProgress()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(42, TargetType.Asteroid, "Rock", "Luminite")
            };

            var action = new BeginLockAction(42, 1.5f);
            var result = TargetingReducer.Reduce(state, action);

            Assert.AreEqual(42, result.LockAcquisition.TargetId);
            Assert.AreEqual(0f, result.LockAcquisition.ElapsedTime);
            Assert.AreEqual(1.5f, result.LockAcquisition.TotalDuration);
            Assert.AreEqual(LockAcquisitionStatus.InProgress, result.LockAcquisition.Status);
            Assert.IsTrue(result.LockAcquisition.IsActive);
        }

        [Test]
        public void BeginLock_ForAlreadyLockedTarget_ReturnsUnchanged()
        {
            var locked = ImmutableArray.Create(
                new TargetLockData(42, TargetType.Asteroid, "Rock", "Luminite"));
            var state = EmptyState with
            {
                Selection = new SelectionData(42, TargetType.Asteroid, "Rock", "Luminite"),
                LockedTargets = locked
            };

            var action = new BeginLockAction(42, 1.5f);
            var result = TargetingReducer.Reduce(state, action);

            Assert.AreSame(state, result);
        }

        // --- LockTickAction ---

        [Test]
        public void LockTick_AdvancesElapsedTime()
        {
            var state = EmptyState with
            {
                LockAcquisition = new LockAcquisitionData(42, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var result = TargetingReducer.Reduce(state, new LockTickAction(0.3f));

            Assert.AreEqual(0.8f, result.LockAcquisition.ElapsedTime, 0.001f);
            Assert.AreEqual(LockAcquisitionStatus.InProgress, result.LockAcquisition.Status);
        }

        [Test]
        public void LockTick_AutoCompletesWhenElapsedReachesTotal()
        {
            var state = EmptyState with
            {
                LockAcquisition = new LockAcquisitionData(42, 1.4f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var result = TargetingReducer.Reduce(state, new LockTickAction(0.2f));

            Assert.AreEqual(LockAcquisitionStatus.Completed, result.LockAcquisition.Status);
        }

        [Test]
        public void LockTick_DoesNothingWhenNoActiveLock()
        {
            var result = TargetingReducer.Reduce(EmptyState, new LockTickAction(0.1f));

            Assert.AreSame(EmptyState, result);
        }

        // --- CompleteLockAction ---

        [Test]
        public void CompleteLock_AppendsToLockedTargets()
        {
            var state = EmptyState with
            {
                Selection = new SelectionData(42, TargetType.Asteroid, "Rock", "Luminite"),
                LockAcquisition = new LockAcquisitionData(42, 1.5f, 1.5f, LockAcquisitionStatus.Completed)
            };

            var result = TargetingReducer.Reduce(state, new CompleteLockAction());

            Assert.AreEqual(1, result.LockedTargets.Length);
            Assert.AreEqual(42, result.LockedTargets[0].TargetId);
            Assert.AreEqual(TargetType.Asteroid, result.LockedTargets[0].TargetType);
            Assert.AreEqual("Rock", result.LockedTargets[0].DisplayName);
            Assert.AreEqual("Luminite", result.LockedTargets[0].TypeLabel);
            Assert.AreEqual(LockAcquisitionStatus.None, result.LockAcquisition.Status);
        }

        [Test]
        public void CompleteLock_IgnoredWhenStatusNotCompleted()
        {
            var state = EmptyState with
            {
                LockAcquisition = new LockAcquisitionData(42, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var result = TargetingReducer.Reduce(state, new CompleteLockAction());

            Assert.AreSame(state, result);
        }

        [Test]
        public void CompleteLock_MaintainsInsertionOrder()
        {
            var locked = ImmutableArray.Create(
                new TargetLockData(10, TargetType.Asteroid, "First", "Luminite"));
            var state = EmptyState with
            {
                Selection = new SelectionData(20, TargetType.Station, "Second", "Station"),
                LockAcquisition = new LockAcquisitionData(20, 1.5f, 1.5f, LockAcquisitionStatus.Completed),
                LockedTargets = locked
            };

            var result = TargetingReducer.Reduce(state, new CompleteLockAction());

            Assert.AreEqual(2, result.LockedTargets.Length);
            Assert.AreEqual(10, result.LockedTargets[0].TargetId);
            Assert.AreEqual(20, result.LockedTargets[1].TargetId);
        }

        // --- CancelLockAction ---

        [Test]
        public void CancelLock_ResetsLockAcquisitionToNone()
        {
            var state = EmptyState with
            {
                LockAcquisition = new LockAcquisitionData(42, 0.5f, 1.5f, LockAcquisitionStatus.InProgress)
            };

            var result = TargetingReducer.Reduce(state, new CancelLockAction());

            Assert.AreEqual(LockAcquisitionStatus.None, result.LockAcquisition.Status);
            Assert.AreEqual(-1, result.LockAcquisition.TargetId);
        }

        // --- UnlockTargetAction ---

        [Test]
        public void UnlockTarget_RemovesSpecificTarget()
        {
            var locked = ImmutableArray.Create(
                new TargetLockData(10, TargetType.Asteroid, "A", "Luminite"),
                new TargetLockData(20, TargetType.Station, "B", "Station"),
                new TargetLockData(30, TargetType.Asteroid, "C", "Ferrox"));
            var state = EmptyState with { LockedTargets = locked };

            var result = TargetingReducer.Reduce(state, new UnlockTargetAction(20));

            Assert.AreEqual(2, result.LockedTargets.Length);
            Assert.AreEqual(10, result.LockedTargets[0].TargetId);
            Assert.AreEqual(30, result.LockedTargets[1].TargetId);
        }

        [Test]
        public void UnlockTarget_ReturnsUnchangedWhenNotFound()
        {
            var locked = ImmutableArray.Create(
                new TargetLockData(10, TargetType.Asteroid, "A", "Luminite"));
            var state = EmptyState with { LockedTargets = locked };

            var result = TargetingReducer.Reduce(state, new UnlockTargetAction(99));

            Assert.AreSame(state, result);
        }

        // --- ClearAllLocksAction ---

        [Test]
        public void ClearAllLocks_EmptiesLockedTargetsAndResetsAll()
        {
            var locked = ImmutableArray.Create(
                new TargetLockData(10, TargetType.Asteroid, "A", "Luminite"),
                new TargetLockData(20, TargetType.Station, "B", "Station"));
            var state = EmptyState with
            {
                Selection = new SelectionData(10, TargetType.Asteroid, "A", "Luminite"),
                LockAcquisition = new LockAcquisitionData(30, 0.5f, 1.5f, LockAcquisitionStatus.InProgress),
                LockedTargets = locked
            };

            var result = TargetingReducer.Reduce(state, new ClearAllLocksAction());

            Assert.AreEqual(0, result.LockedTargets.Length);
            Assert.IsFalse(result.Selection.HasSelection);
            Assert.AreEqual(LockAcquisitionStatus.None, result.LockAcquisition.Status);
        }
    }
}
