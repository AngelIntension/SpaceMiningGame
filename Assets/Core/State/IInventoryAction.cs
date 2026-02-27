namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Marker interface for inventory actions. Routed to InventoryReducer.
    /// See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public interface IInventoryAction : IGameAction { }
}
