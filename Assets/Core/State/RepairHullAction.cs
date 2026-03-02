namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Ship action to set hull integrity. Dispatched by CompositeReducer during repair.
    /// See Spec 006 US4: Basic Repair.
    /// </summary>
    public sealed record RepairHullAction(
        float NewIntegrity
    ) : IShipAction;
}
