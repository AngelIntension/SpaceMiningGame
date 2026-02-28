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

        /// <summary>Type of the targeted object (Asteroid, Station, None). See spec 004.</summary>
        public readonly VoidHarvest.Core.Extensions.TargetType TargetType;

        /// <summary>
        /// Create a radial menu requested event. See MVP-03: Target selection and alignment.
        /// </summary>
        public RadialMenuRequestedEvent(int targetId, VoidHarvest.Core.Extensions.TargetType targetType = VoidHarvest.Core.Extensions.TargetType.None)
        {
            TargetId = targetId;
            TargetType = targetType;
        }
    }
}
