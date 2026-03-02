using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Immutable state for a single refining job. Tracks lifecycle from Active to Completed.
    /// See Spec 006: Station Services.
    /// </summary>
    public sealed record RefiningJobState(
        string JobId,
        string OreId,
        int InputQuantity,
        float StartTime,
        float TotalDuration,
        int CreditCostPaid,
        RefiningJobStatus Status,
        ImmutableArray<RefiningOutputConfig> OutputConfigs,
        ImmutableArray<MaterialOutput> GeneratedOutputs
    )
    {
        /// <summary>
        /// Returns progress as a float in [0, 1]. 1.0 if Completed.
        /// </summary>
        public float Progress(float currentTime)
        {
            if (Status == RefiningJobStatus.Completed) return 1f;
            if (TotalDuration <= 0f) return 1f;
            float elapsed = currentTime - StartTime;
            float progress = elapsed / TotalDuration;
            return progress < 0f ? 0f : progress > 1f ? 1f : progress;
        }

        /// <summary>
        /// Returns remaining time in seconds. 0 if Completed.
        /// </summary>
        public float RemainingTime(float currentTime)
        {
            if (Status == RefiningJobStatus.Completed) return 0f;
            float remaining = (StartTime + TotalDuration) - currentTime;
            return remaining < 0f ? 0f : remaining;
        }

        public static readonly RefiningJobState Empty = new(
            string.Empty, string.Empty, 0, 0f, 0f, 0,
            RefiningJobStatus.Active,
            ImmutableArray<RefiningOutputConfig>.Empty,
            ImmutableArray<MaterialOutput>.Empty
        );
    }
}
