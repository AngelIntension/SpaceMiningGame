using System.Collections.Immutable;
using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Status of an in-progress lock acquisition.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public enum LockAcquisitionStatus
    {
        None = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Snapshot of the currently selected (not locked) target.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public readonly struct SelectionData
    {
        public readonly int TargetId;
        public readonly TargetType TargetType;
        public readonly string DisplayName;
        public readonly string TypeLabel;

        public bool HasSelection => TargetId >= 0;

        public static readonly SelectionData None = new SelectionData(-1, TargetType.None, string.Empty, string.Empty);

        public SelectionData(int targetId, TargetType targetType, string displayName, string typeLabel)
        {
            TargetId = targetId;
            TargetType = targetType;
            DisplayName = displayName ?? string.Empty;
            TypeLabel = typeLabel ?? string.Empty;
        }
    }

    /// <summary>
    /// State of an active lock-in-progress.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public readonly struct LockAcquisitionData
    {
        public readonly int TargetId;
        public readonly float ElapsedTime;
        public readonly float TotalDuration;
        public readonly LockAcquisitionStatus Status;

        public float Progress => TotalDuration > 0f ? UnityEngine.Mathf.Clamp01(ElapsedTime / TotalDuration) : 0f;
        public bool IsActive => Status == LockAcquisitionStatus.InProgress;

        public static readonly LockAcquisitionData None = new LockAcquisitionData(-1, 0f, 0f, LockAcquisitionStatus.None);

        public LockAcquisitionData(int targetId, float elapsedTime, float totalDuration, LockAcquisitionStatus status)
        {
            TargetId = targetId;
            ElapsedTime = elapsedTime;
            TotalDuration = totalDuration;
            Status = status;
        }
    }

    /// <summary>
    /// A confirmed target lock entry.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public readonly struct TargetLockData
    {
        public readonly int TargetId;
        public readonly TargetType TargetType;
        public readonly string DisplayName;
        public readonly string TypeLabel;

        public TargetLockData(int targetId, TargetType targetType, string displayName, string typeLabel)
        {
            TargetId = targetId;
            TargetType = targetType;
            DisplayName = displayName ?? string.Empty;
            TypeLabel = typeLabel ?? string.Empty;
        }
    }

    /// <summary>
    /// Root targeting state slice. Selection + lock acquisition + confirmed locks.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public sealed record TargetingState(
        SelectionData Selection,
        LockAcquisitionData LockAcquisition,
        ImmutableArray<TargetLockData> LockedTargets
    )
    {
        public static readonly TargetingState Empty = new(
            SelectionData.None,
            LockAcquisitionData.None,
            ImmutableArray<TargetLockData>.Empty
        );
    }
}
