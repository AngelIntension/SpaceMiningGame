using System.Collections.Immutable;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Targeting.Data;

namespace VoidHarvest.Features.Targeting.Systems
{
    /// <summary>
    /// Pure static reducer for TargetingState. Handles all 8 targeting actions.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public static class TargetingReducer
    {
        public static TargetingState Reduce(TargetingState state, ITargetingAction action)
            => action switch
            {
                SelectTargetAction a => HandleSelectTarget(state, a),
                ClearSelectionAction => HandleClearSelection(state),
                BeginLockAction a => HandleBeginLock(state, a),
                LockTickAction a => HandleLockTick(state, a),
                CompleteLockAction => HandleCompleteLock(state),
                CancelLockAction => HandleCancelLock(state),
                UnlockTargetAction a => HandleUnlockTarget(state, a),
                ClearAllLocksAction => HandleClearAllLocks(),
                _ => state
            };

        private static TargetingState HandleSelectTarget(TargetingState state, SelectTargetAction a)
        {
            var newSelection = new SelectionData(a.TargetId, a.TargetType, a.DisplayName, a.TypeLabel);

            // Cancel active lock if target changed
            var lockAcquisition = state.LockAcquisition;
            if (lockAcquisition.IsActive && lockAcquisition.TargetId != a.TargetId)
            {
                lockAcquisition = LockAcquisitionData.None;
            }

            return state with
            {
                Selection = newSelection,
                LockAcquisition = lockAcquisition
            };
        }

        private static TargetingState HandleClearSelection(TargetingState state)
        {
            return state with
            {
                Selection = SelectionData.None,
                LockAcquisition = LockAcquisitionData.None
            };
        }

        private static TargetingState HandleBeginLock(TargetingState state, BeginLockAction a)
        {
            // Prevent duplicate lock
            for (int i = 0; i < state.LockedTargets.Length; i++)
            {
                if (state.LockedTargets[i].TargetId == a.TargetId)
                    return state;
            }

            return state with
            {
                LockAcquisition = new LockAcquisitionData(
                    a.TargetId,
                    0f,
                    a.Duration,
                    LockAcquisitionStatus.InProgress)
            };
        }

        private static TargetingState HandleLockTick(TargetingState state, LockTickAction a)
        {
            if (!state.LockAcquisition.IsActive)
                return state;

            var acq = state.LockAcquisition;
            float newElapsed = acq.ElapsedTime + a.DeltaTime;
            var newStatus = newElapsed >= acq.TotalDuration
                ? LockAcquisitionStatus.Completed
                : LockAcquisitionStatus.InProgress;

            return state with
            {
                LockAcquisition = new LockAcquisitionData(
                    acq.TargetId,
                    newElapsed,
                    acq.TotalDuration,
                    newStatus)
            };
        }

        private static TargetingState HandleCompleteLock(TargetingState state)
        {
            if (state.LockAcquisition.Status != LockAcquisitionStatus.Completed)
                return state;

            var acq = state.LockAcquisition;
            var sel = state.Selection;
            var newLock = new TargetLockData(
                acq.TargetId,
                sel.TargetType,
                sel.DisplayName,
                sel.TypeLabel);

            return state with
            {
                LockAcquisition = LockAcquisitionData.None,
                LockedTargets = state.LockedTargets.Add(newLock)
            };
        }

        private static TargetingState HandleCancelLock(TargetingState state)
        {
            return state with
            {
                LockAcquisition = LockAcquisitionData.None
            };
        }

        private static TargetingState HandleUnlockTarget(TargetingState state, UnlockTargetAction a)
        {
            var builder = ImmutableArray.CreateBuilder<TargetLockData>(state.LockedTargets.Length);
            bool found = false;
            for (int i = 0; i < state.LockedTargets.Length; i++)
            {
                if (state.LockedTargets[i].TargetId == a.TargetId)
                {
                    found = true;
                    continue;
                }
                builder.Add(state.LockedTargets[i]);
            }

            if (!found) return state;

            return state with { LockedTargets = builder.ToImmutable() };
        }

        private static TargetingState HandleClearAllLocks()
        {
            return TargetingState.Empty;
        }
    }
}
