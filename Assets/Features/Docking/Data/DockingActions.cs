using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Docking.Data
{
    /// <summary>Start docking sequence toward a station port.</summary>
    public sealed record BeginDockingAction(
        int StationId,
        float3 PortPosition,
        quaternion PortRotation
    ) : IDockingAction;

    /// <summary>Snap finished, ship is docked.</summary>
    public sealed record CompleteDockingAction(int StationId) : IDockingAction;

    /// <summary>Player cancelled docking or target lost.</summary>
    public sealed record CancelDockingAction : IDockingAction;

    /// <summary>Player initiated undock.</summary>
    public sealed record BeginUndockingAction : IDockingAction;

    /// <summary>Ship reached clearance distance after undocking.</summary>
    public sealed record CompleteUndockingAction : IDockingAction;
}
