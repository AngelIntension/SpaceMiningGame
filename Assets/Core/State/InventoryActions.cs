namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Add a quantity of a resource to inventory.
    /// Rejected if volume would exceed MaxVolume or new stack would exceed MaxSlots.
    /// See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public sealed record AddResourceAction(
        string ResourceId,
        int Quantity,
        float VolumePerUnit
    ) : IInventoryAction;

    /// <summary>
    /// Remove a quantity of a resource from inventory.
    /// Rejected if insufficient stock. Removes stack entry when quantity reaches zero.
    /// See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public sealed record RemoveResourceAction(
        string ResourceId,
        int Quantity
    ) : IInventoryAction;
}
