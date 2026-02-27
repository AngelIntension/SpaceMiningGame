namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Published when a mining beam activates on an asteroid.
    /// See MVP-05: Mining beam connects within 500ms.
    /// </summary>
    public readonly struct MiningStartedEvent
    {
        /// <summary>Instance ID of the targeted asteroid. See MVP-05.</summary>
        public readonly int AsteroidId;
        /// <summary>Ore type identifier being mined. See MVP-05.</summary>
        public readonly string OreId;

        /// <summary>
        /// Create a mining started event. See MVP-05: Mining beam and yield.
        /// </summary>
        public MiningStartedEvent(int asteroidId, string oreId)
        {
            AsteroidId = asteroidId;
            OreId = oreId;
        }
    }
}
