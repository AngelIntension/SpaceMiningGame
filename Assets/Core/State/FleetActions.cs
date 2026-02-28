namespace VoidHarvest.Core.State
{
    /// <summary>Set FleetState.DockedAtStation when docking completes.</summary>
    public sealed record DockAtStationAction(int StationId) : IFleetAction;

    /// <summary>Clear FleetState.DockedAtStation when undocking completes.</summary>
    public sealed record UndockFromStationAction : IFleetAction;
}
