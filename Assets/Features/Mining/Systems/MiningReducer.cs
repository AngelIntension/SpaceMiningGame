using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Mining.Systems
{
    /// <summary>
    /// Pure static reducer for mining session state.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public static class MiningReducer
    {
        /// <summary>
        /// Reduce mining session state by applying a mining action. See MVP-05: Mining beam and yield.
        /// </summary>
        public static MiningSessionState Reduce(MiningSessionState state, IMiningAction action)
            => action switch
            {
                BeginMiningAction a => state with
                {
                    TargetAsteroidId = Option<int>.Some(a.AsteroidId),
                    ActiveOreId = Option<string>.Some(a.OreId),
                    BeamEnergy = 1.0f,
                    YieldAccumulator = 0f,
                    MiningDuration = 0f,
                    DepletionFraction = 0f
                },
                MiningTickAction a => ComputeMiningTick(state, a),
                MiningDepletionTickAction a => state with { DepletionFraction = a.DepletionFraction },
                StopMiningAction => MiningSessionState.Empty,
                _ => state
            };

        private static MiningSessionState ComputeMiningTick(MiningSessionState state, MiningTickAction action)
        {
            var yield = CalculateYield(
                state.ActiveOreId.GetValueOrDefault(""),
                action.ShipMiningPower,
                action.BaseYield,
                action.Hardness,
                action.Depth,
                action.DeltaTime);

            float newAccumulator = state.YieldAccumulator + yield.WholeUnitsYielded + yield.RemainingFraction;
            float newDuration = state.MiningDuration + action.DeltaTime;

            return state with
            {
                YieldAccumulator = newAccumulator,
                MiningDuration = newDuration
            };
        }

        /// <summary>
        /// Pure yield calculation. Formula: (miningPower * baseYield * deltaTime) / (hardness * (1 + depth))
        /// Zero hardness guard returns zero yield.
        /// </summary>
        public static MiningYieldResult CalculateYield(
            string oreId, float miningPower, float baseYield,
            float hardness, float depth, float deltaTime)
        {
            if (hardness <= 0f)
                return new MiningYieldResult(oreId, 0, 0f);

            float amount = (miningPower * baseYield * deltaTime) / (hardness * (1f + depth));
            int wholeUnits = (int)System.Math.Floor(amount);
            float fraction = amount - wholeUnits;

            return new MiningYieldResult(oreId, wholeUnits, fraction);
        }
    }
}
