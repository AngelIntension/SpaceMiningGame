using Unity.Mathematics;
using VoidHarvest.Core.Extensions;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Phase of the docking sequence. See spec 004.
    /// </summary>
    public enum DockingPhase
    {
        None = 0,
        Approaching = 1,
        Aligning = 5,
        Snapping = 2,
        Docked = 3,
        Undocking = 4
    }

    /// <summary>
    /// Immutable docking state managed by DockingReducer.
    /// Tracks the current docking sequence phase and target information.
    /// </summary>
    public sealed record DockingState(
        DockingPhase Phase,
        Option<int> TargetStationId,
        Option<float3> DockingPortPosition,
        Option<quaternion> DockingPortRotation
    )
    {
        public static readonly DockingState Empty = new(
            DockingPhase.None, default, default, default
        );

        public bool IsDocked => Phase == DockingPhase.Docked;
        public bool IsInProgress => Phase == DockingPhase.Approaching || Phase == DockingPhase.Aligning || Phase == DockingPhase.Snapping;
    }
}
