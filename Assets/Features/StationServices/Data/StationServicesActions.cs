using System.Collections.Immutable;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.StationServices.Data
{
    // --- Single-slice actions (handled by StationServicesReducer) ---

    public sealed record SellResourceAction(
        int StationId, string ResourceId, int Quantity, int PricePerUnit
    ) : IStationServicesAction;

    public sealed record StartRefiningJobAction(
        int StationId, string OreId, int InputQuantity, int TotalCost,
        float TotalDuration, ImmutableArray<RefiningOutputConfig> OutputConfigs,
        int MaxActiveSlots, float StartTime
    ) : IStationServicesAction;

    public sealed record CompleteRefiningJobAction(
        int StationId, string JobId, ImmutableArray<MaterialOutput> GeneratedOutputs
    ) : IStationServicesAction;

    public sealed record CollectRefiningJobAction(
        int StationId, string JobId
    ) : IStationServicesAction;

    public sealed record AddToStationStorageAction(
        int StationId, string ResourceId, int Quantity, float VolumePerUnit
    ) : IStationServicesAction;

    public sealed record RemoveFromStationStorageAction(
        int StationId, string ResourceId, int Quantity
    ) : IStationServicesAction;

    public sealed record InitializeStationStorageAction(
        int StationId
    ) : IStationServicesAction;

    public sealed record SetCreditsAction(
        int NewBalance
    ) : IStationServicesAction;

    // --- Cross-cutting actions (handled in CompositeReducer) ---

    public sealed record TransferToStationAction(
        int StationId, string ResourceId, int Quantity, float VolumePerUnit
    ) : IStationServicesAction;

    public sealed record TransferToShipAction(
        int StationId, string ResourceId, int Quantity, float VolumePerUnit
    ) : IStationServicesAction;

    public sealed record RepairShipAction(
        int Cost, float NewIntegrity
    ) : IStationServicesAction;

    /// <summary>
    /// Ship action to set hull integrity. Dispatched by CompositeReducer during repair.
    /// </summary>
    public sealed record RepairHullAction(
        float NewIntegrity
    ) : IShipAction;
}
