using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Per-station unlimited storage inventory. Reuses ResourceStack from Core.State.
    /// See Spec 006: Station Services.
    /// </summary>
    public sealed record StationStorageState(
        ImmutableDictionary<string, ResourceStack> Stacks
    )
    {
        public static readonly StationStorageState Empty = new(
            ImmutableDictionary<string, ResourceStack>.Empty
        );
    }
}
