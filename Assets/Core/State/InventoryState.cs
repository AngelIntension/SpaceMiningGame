using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Immutable inventory state. See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public sealed record InventoryState(
        ImmutableDictionary<string, ResourceStack> Stacks,
        int MaxSlots,
        float MaxVolume,
        float CurrentVolume
    )
    {
        public static readonly InventoryState Empty = new(
            ImmutableDictionary<string, ResourceStack>.Empty, 20, 100f, 0f
        );
    }

    /// <summary>
    /// A stack of a single resource type in inventory.
    /// </summary>
    public readonly struct ResourceStack
    {
        /// <summary>Unique identifier for the resource type. See MVP-06: Inventory management.</summary>
        public readonly string ResourceId;

        /// <summary>Number of units in this stack. See MVP-06: Inventory management.</summary>
        public readonly int Quantity;

        /// <summary>Cargo volume consumed per unit. See MVP-06: Inventory management.</summary>
        public readonly float VolumePerUnit;

        /// <summary>
        /// Create a resource stack with the given resource, quantity, and volume per unit. See MVP-06: Inventory management.
        /// </summary>
        public ResourceStack(string resourceId, int quantity, float volumePerUnit)
        {
            ResourceId = resourceId;
            Quantity = quantity;
            VolumePerUnit = volumePerUnit;
        }
    }
}
