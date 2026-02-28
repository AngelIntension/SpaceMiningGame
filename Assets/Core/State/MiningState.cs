using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Immutable mining session state. See MVP-05: Mining beam and yield.
    /// </summary>
    public sealed record MiningSessionState(
        Option<int> TargetAsteroidId,
        Option<string> ActiveOreId,
        float BeamEnergy,
        float YieldAccumulator,
        float MiningDuration,
        float BeamMaxRange,
        float DepletionFraction
    )
    {
        public static readonly MiningSessionState Empty = new(
            default, default, 0f, 0f, 0f, 50f, 0f
        );
    }

    /// <summary>
    /// Result of a mining yield calculation.
    /// </summary>
    public sealed record MiningYieldResult(
        string OreId,
        int WholeUnitsYielded,
        float RemainingFraction
    );
}
