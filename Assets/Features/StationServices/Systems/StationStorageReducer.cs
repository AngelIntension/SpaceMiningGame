using VoidHarvest.Core.State;

namespace VoidHarvest.Features.StationServices.Systems
{
    /// <summary>
    /// Pure static reducer for station storage operations.
    /// See Spec 006: Station Services.
    /// </summary>
    public static class StationStorageReducer
    {
        public static StationStorageState AddResource(StationStorageState state, string resourceId, int quantity, float volumePerUnit)
        {
            if (quantity <= 0) return state;

            if (state.Stacks.TryGetValue(resourceId, out var existing))
            {
                var updated = new ResourceStack(resourceId, existing.Quantity + quantity, volumePerUnit);
                return state with { Stacks = state.Stacks.SetItem(resourceId, updated) };
            }

            var newStack = new ResourceStack(resourceId, quantity, volumePerUnit);
            return state with { Stacks = state.Stacks.Add(resourceId, newStack) };
        }

        public static StationStorageState RemoveResource(StationStorageState state, string resourceId, int quantity)
        {
            if (!state.Stacks.TryGetValue(resourceId, out var existing))
                return state;

            if (existing.Quantity < quantity)
                return state;

            int newQty = existing.Quantity - quantity;
            if (newQty <= 0)
                return state with { Stacks = state.Stacks.Remove(resourceId) };

            var updated = new ResourceStack(resourceId, newQty, existing.VolumePerUnit);
            return state with { Stacks = state.Stacks.SetItem(resourceId, updated) };
        }
    }
}
