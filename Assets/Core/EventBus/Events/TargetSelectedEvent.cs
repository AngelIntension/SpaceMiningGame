namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when the player selects a target (asteroid, station, etc.).
    /// See MVP-03: Target selection and alignment.
    /// </summary>
    public readonly struct TargetSelectedEvent
    {
        /// <summary>Instance ID of the selected target object. See MVP-03.</summary>
        public readonly int TargetId;

        /// <summary>
        /// Create a target selected event. See MVP-03: Mouse targeting.
        /// </summary>
        public TargetSelectedEvent(int targetId)
        {
            TargetId = targetId;
        }
    }
}
