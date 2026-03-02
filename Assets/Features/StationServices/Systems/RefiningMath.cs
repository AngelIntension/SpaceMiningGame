using System.Collections.Immutable;
using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.StationServices.Systems
{
    /// <summary>
    /// Pure static functions for refining yield calculation.
    /// Per-unit rolling with deterministic Unity.Mathematics.Random.
    /// See Spec 006: Station Services.
    /// </summary>
    public static class RefiningMath
    {
        /// <summary>
        /// Calculate material outputs for a completed refining job.
        /// Per-unit rolling: for each input unit, rolls an independent offset per output config.
        /// </summary>
        public static ImmutableArray<MaterialOutput> CalculateOutputs(
            ImmutableArray<RefiningOutputConfig> configs, int inputQuantity, ref Random random)
        {
            var builder = ImmutableArray.CreateBuilder<MaterialOutput>(configs.Length);

            for (int c = 0; c < configs.Length; c++)
            {
                var config = configs[c];
                int totalYield = 0;

                for (int u = 0; u < inputQuantity; u++)
                {
                    // NextInt(min, max) is [min, max) exclusive upper bound
                    int offset = random.NextInt(config.VarianceMin, config.VarianceMax + 1);
                    int unitYield = config.BaseYieldPerUnit + offset;
                    if (unitYield < 0) unitYield = 0;
                    totalYield += unitYield;
                }

                builder.Add(new MaterialOutput(config.MaterialId, totalYield));
            }

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Calculate total refining job duration in seconds.
        /// </summary>
        public static float CalculateJobDuration(int inputQuantity, float baseProcessingTimePerUnit, float speedMultiplier)
        {
            float effectiveMultiplier = speedMultiplier < 0.01f ? 0.01f : speedMultiplier;
            return (inputQuantity * baseProcessingTimePerUnit) / effectiveMultiplier;
        }

        /// <summary>
        /// Calculate total credit cost for a refining job.
        /// </summary>
        public static int CalculateJobCost(int inputQuantity, int creditCostPerUnit)
        {
            return inputQuantity * creditCostPerUnit;
        }
    }
}
