namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Ship flight automation modes. See MVP-01, MVP-03.
    /// </summary>
    public enum ShipFlightMode
    {
        /// <summary>No active thrust input or auto-pilot.</summary>
        Idle,
        /// <summary>Player is providing manual WASD/QE thrust input. See MVP-01.</summary>
        ManualThrust,
        /// <summary>Auto-rotate to face a target point. See MVP-04.</summary>
        AlignToPoint,
        /// <summary>Auto-pilot: fly toward target, decelerate at distance. See MVP-04.</summary>
        Approach,
        /// <summary>Auto-pilot: maintain lateral orbit around target. See MVP-04.</summary>
        Orbit,
        /// <summary>Auto-pilot: maintain fixed radial distance from target. See MVP-04.</summary>
        KeepAtRange,
        /// <summary>Ship is in automatic docking sequence (approach/snap). See spec 004.</summary>
        Docking,
        /// <summary>Ship is locked at a station docking port. See spec 004.</summary>
        Docked,
        /// <summary>High-speed warp travel. Phase 1+.</summary>
        Warp
    }
}
