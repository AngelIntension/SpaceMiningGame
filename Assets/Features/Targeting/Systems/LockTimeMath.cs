using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Features.Targeting.Systems
{
    /// <summary>
    /// Pure static class for lock time calculation.
    /// V1: returns baseLockTime directly. TargetInfo parameter enables future extensibility.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public static class LockTimeMath
    {
        /// <summary>
        /// Calculate the lock acquisition time for a target.
        /// V1: returns baseLockTime. Future: distance, size, sensor upgrade factors.
        /// </summary>
        public static float CalculateLockTime(float baseLockTime, TargetInfo target)
        {
            return baseLockTime;
        }
    }
}
