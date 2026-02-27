namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when a mining beam deactivates.
    /// See MVP-05: Mining beam feedback.
    /// </summary>
    public readonly struct MiningStoppedEvent
    {
        /// <summary>Instance ID of the asteroid that was being mined. See MVP-05.</summary>
        public readonly int AsteroidId;
        /// <summary>Reason the mining session ended. See MVP-05.</summary>
        public readonly StopReason Reason;

        /// <summary>
        /// Create a mining stopped event. See MVP-05: Mining beam and yield.
        /// </summary>
        public MiningStoppedEvent(int asteroidId, StopReason reason)
        {
            AsteroidId = asteroidId;
            Reason = reason;
        }
    }
}
