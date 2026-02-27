using System.Collections.Immutable;
using Unity.Mathematics;
using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// World-level state for stations and asteroid field initialization data.
    /// </summary>
    public sealed record WorldState(
        ImmutableArray<StationData> Stations,
        float WorldTime
    );

    /// <summary>
    /// Station data for docking, trading, repairing.
    /// Stub in MVP — no stations implemented yet.
    /// </summary>
    public sealed record StationData(
        int Id,
        float3 Position,
        string Name,
        ImmutableArray<string> AvailableServices
    );
}
