namespace VoidHarvest.Core.State
{
    /// <summary>See MVP-02: Camera orbit and zoom.</summary>
    public sealed record OrbitAction(float DeltaYaw, float DeltaPitch) : ICameraAction;

    /// <summary>Manual zoom via scroll wheel. See MVP-02.</summary>
    public sealed record ZoomAction(float Delta) : ICameraAction;

    /// <summary>Speed-based auto-zoom. See MVP-02.</summary>
    public sealed record SpeedZoomAction(float NormalizedSpeed) : ICameraAction;

    /// <summary>Toggle free-look mode. See MVP-02.</summary>
    public sealed record ToggleFreeLookAction() : ICameraAction;

    /// <summary>Free-look offset adjustment. See MVP-02.</summary>
    public sealed record FreeLookAction(float DeltaYaw, float DeltaPitch) : ICameraAction;
}
