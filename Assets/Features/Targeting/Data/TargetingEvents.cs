namespace VoidHarvest.Features.Targeting.Data
{
    /// <summary>Lock acquisition completed successfully.</summary>
    public readonly struct TargetLockedEvent
    {
        public readonly int TargetId;
        public readonly string DisplayName;
        public TargetLockedEvent(int targetId, string displayName) { TargetId = targetId; DisplayName = displayName; }
    }

    /// <summary>Target manually dismissed from card.</summary>
    public readonly struct TargetUnlockedEvent
    {
        public readonly int TargetId;
        public TargetUnlockedEvent(int targetId) { TargetId = targetId; }
    }

    /// <summary>Lock cancelled (deselect, out-of-range, destroyed).</summary>
    public readonly struct LockFailedEvent
    {
        public readonly int TargetId;
        public readonly LockFailReason Reason;
        public LockFailedEvent(int targetId, LockFailReason reason) { TargetId = targetId; Reason = reason; }
    }

    /// <summary>Player attempted lock at max capacity.</summary>
    public readonly struct LockSlotsFullEvent
    {
    }

    /// <summary>Locked target destroyed (asteroid depleted).</summary>
    public readonly struct TargetLostEvent
    {
        public readonly int TargetId;
        public TargetLostEvent(int targetId) { TargetId = targetId; }
    }

    /// <summary>All locks cleared (docking, ship swap).</summary>
    public readonly struct AllLocksClearedEvent
    {
    }
}
