using System.Collections.Immutable;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.Resources.Systems
{
    /// <summary>
    /// Pure static reducer for inventory state.
    /// See MVP-06: Inventory tracks correct quantities.
    /// </summary>
    public static class InventoryReducer
    {
        /// <summary>
        /// Reduce inventory state by applying an inventory action. See MVP-06: Inventory tracks correct quantities.
        /// </summary>
        public static InventoryState Reduce(InventoryState state, IInventoryAction action)
            => action switch
            {
                AddResourceAction a => AddResource(state, a),
                RemoveResourceAction a => RemoveResource(state, a),
                _ => state
            };

        private static InventoryState AddResource(InventoryState state, AddResourceAction action)
        {
            if (action.Quantity <= 0)
                return state;

            float addedVolume = action.Quantity * action.VolumePerUnit;
            float newVolume = state.CurrentVolume + addedVolume;

            // Reject if volume would exceed capacity
            if (newVolume > state.MaxVolume)
                return state;

            if (state.Stacks.TryGetValue(action.ResourceId, out var existing))
            {
                // Update existing stack
                var updatedStack = new ResourceStack(
                    action.ResourceId,
                    existing.Quantity + action.Quantity,
                    action.VolumePerUnit);

                return state with
                {
                    Stacks = state.Stacks.SetItem(action.ResourceId, updatedStack),
                    CurrentVolume = newVolume
                };
            }
            else
            {
                // New stack — check slot limit
                if (state.Stacks.Count >= state.MaxSlots)
                    return state;

                var newStack = new ResourceStack(action.ResourceId, action.Quantity, action.VolumePerUnit);

                return state with
                {
                    Stacks = state.Stacks.Add(action.ResourceId, newStack),
                    CurrentVolume = newVolume
                };
            }
        }

        private static InventoryState RemoveResource(InventoryState state, RemoveResourceAction action)
        {
            if (!state.Stacks.TryGetValue(action.ResourceId, out var existing))
                return state;

            if (existing.Quantity < action.Quantity)
                return state;

            float removedVolume = action.Quantity * existing.VolumePerUnit;
            int newQty = existing.Quantity - action.Quantity;

            if (newQty <= 0)
            {
                // Remove the stack entirely
                return state with
                {
                    Stacks = state.Stacks.Remove(action.ResourceId),
                    CurrentVolume = state.CurrentVolume - (existing.Quantity * existing.VolumePerUnit)
                };
            }

            var updatedStack = new ResourceStack(action.ResourceId, newQty, existing.VolumePerUnit);

            return state with
            {
                Stacks = state.Stacks.SetItem(action.ResourceId, updatedStack),
                CurrentVolume = state.CurrentVolume - removedVolume
            };
        }
    }
}
