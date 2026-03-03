using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Targeting.Data
{
    /// <summary>
    /// Marker interface for all targeting actions. Routed by CompositeReducer.
    /// See Spec 007: In-Flight Targeting.
    /// </summary>
    public interface ITargetingAction : IGameAction { }

    /// <summary>Select a target (asteroid or station).</summary>
    public sealed record SelectTargetAction(
        int TargetId,
        TargetType TargetType,
        string DisplayName,
        string TypeLabel
    ) : ITargetingAction;

    /// <summary>Clear the current selection.</summary>
    public sealed record ClearSelectionAction() : ITargetingAction;

    /// <summary>Begin timed lock acquisition on selected target.</summary>
    public sealed record BeginLockAction(
        int TargetId,
        float Duration
    ) : ITargetingAction;

    /// <summary>Advance lock acquisition timer by delta time.</summary>
    public sealed record LockTickAction(
        float DeltaTime
    ) : ITargetingAction;

    /// <summary>Complete active lock acquisition, converting to a confirmed lock.</summary>
    public sealed record CompleteLockAction() : ITargetingAction;

    /// <summary>Cancel active lock acquisition.</summary>
    public sealed record CancelLockAction() : ITargetingAction;

    /// <summary>Remove a specific target from locked targets.</summary>
    public sealed record UnlockTargetAction(
        int TargetId
    ) : ITargetingAction;

    /// <summary>Clear all locked targets (dock/undock/ship swap).</summary>
    public sealed record ClearAllLocksAction() : ITargetingAction;
}
