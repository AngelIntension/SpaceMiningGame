namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Begin a mining session on a target asteroid.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed record BeginMiningAction(int AsteroidId, string OreId) : IMiningAction;

    /// <summary>
    /// Tick the mining session with physics parameters.
    /// Yield formula: (ShipMiningPower * BaseYield * DeltaTime) / (Hardness * (1 + Depth))
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed record MiningTickAction(
        float DeltaTime,
        float BaseYield,
        float Hardness,
        float Depth,
        float ShipMiningPower
    ) : IMiningAction;

    /// <summary>
    /// Stop the current mining session.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed record StopMiningAction() : IMiningAction;

    /// <summary>
    /// Updates the depletion fraction in MiningSessionState for HUD display.
    /// Dispatched by MiningActionDispatchSystem each frame during active mining.
    /// </summary>
    public sealed record MiningDepletionTickAction(float DepletionFraction) : IMiningAction;
}
