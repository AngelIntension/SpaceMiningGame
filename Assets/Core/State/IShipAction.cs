namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Marker interface for ship actions. Routed to ShipStateReducer.
    /// See MVP-01: 6DOF Newtonian flight.
    /// </summary>
    public interface IShipAction : IGameAction { }
}
