namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Marker interface for all station services actions. Routed by CompositeReducer.
    /// See Spec 006: Station Services.
    /// </summary>
    public interface IStationServicesAction : IGameAction { }
}
