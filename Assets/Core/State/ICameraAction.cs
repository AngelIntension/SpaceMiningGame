namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Marker interface for camera actions. Routed to CameraReducer.
    /// See MVP-02: Camera orbit and zoom.
    /// </summary>
    public interface ICameraAction : IGameAction { }
}
