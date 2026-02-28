using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Docking.Systems
{
    /// <summary>
    /// Pure reducer: (DockingState, IDockingAction) → DockingState.
    /// All docking state transitions are explicit and validated.
    /// </summary>
    public static class DockingReducer
    {
        public static DockingState Reduce(DockingState state, IDockingAction action)
            => action switch
            {
                BeginDockingAction a when state.Phase == DockingPhase.None =>
                    new DockingState(
                        DockingPhase.Approaching,
                        Option<int>.Some(a.StationId),
                        Option<Unity.Mathematics.float3>.Some(a.PortPosition),
                        Option<Unity.Mathematics.quaternion>.Some(a.PortRotation)
                    ),

                CompleteDockingAction when state.Phase == DockingPhase.Approaching =>
                    state with { Phase = DockingPhase.Docked },

                CancelDockingAction when state.Phase == DockingPhase.Approaching =>
                    DockingState.Empty,

                BeginUndockingAction when state.Phase == DockingPhase.Docked =>
                    state with { Phase = DockingPhase.Undocking },

                CompleteUndockingAction when state.Phase == DockingPhase.Undocking =>
                    DockingState.Empty,

                _ => state
            };
    }
}
