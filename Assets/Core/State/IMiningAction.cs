namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Marker interface for mining actions. Routed to MiningReducer.
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public interface IMiningAction : IGameAction { }
}
