namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when the player requests a radial context menu on the current target.
    /// See MVP-03: Target selection and alignment.
    /// </summary>
    public readonly struct RadialMenuRequestedEvent
    {
        /// <summary>Instance ID of the target for the radial menu. See MVP-03.</summary>
        public readonly int TargetId;

        /// <summary>
        /// Create a radial menu requested event. See MVP-03: Target selection and alignment.
        /// </summary>
        public RadialMenuRequestedEvent(int targetId)
        {
            TargetId = targetId;
        }
    }
}
