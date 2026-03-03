namespace VoidHarvest.Features.Targeting.Data
{
    /// <summary>
    /// Reason a target lock failed.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public enum LockFailReason
    {
        Deselected = 0,
        OutOfRange = 1,
        TargetDestroyed = 2
    }
}
