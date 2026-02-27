namespace VoidHarvest.Core.EventBus.Events
{
    /// <summary>
    /// Reason a mining session ended.
    /// </summary>
    public enum StopReason
    {
        /// <summary>Player manually stopped mining. See MVP-05.</summary>
        PlayerStopped,
        /// <summary>Ship moved beyond beam max range. See MVP-05.</summary>
        OutOfRange,
        /// <summary>Asteroid mass depleted to zero. See MVP-05.</summary>
        AsteroidDepleted,
        /// <summary>Inventory volume capacity reached. See MVP-06.</summary>
        CargoFull
    }
}
