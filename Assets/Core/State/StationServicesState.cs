using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Top-level station services state. Contains player credits, per-station storage, and per-station refining jobs.
    /// Replaces the RefiningState stub in GameLoopState.
    /// See Spec 006: Station Services.
    /// </summary>
    public sealed record StationServicesState(
        int Credits,
        ImmutableDictionary<int, StationStorageState> StationStorages,
        ImmutableDictionary<int, ImmutableArray<RefiningJobState>> RefiningJobs
    )
    {
        public static readonly StationServicesState Empty = new(
            0,
            ImmutableDictionary<int, StationStorageState>.Empty,
            ImmutableDictionary<int, ImmutableArray<RefiningJobState>>.Empty
        );
    }
}
