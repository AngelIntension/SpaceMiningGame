using System.Collections.Immutable;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Tech tree state. Stub — Phase 1+.
    /// </summary>
    public sealed record TechTreeState(
        ImmutableDictionary<string, TechNodeStatus> Nodes,
        ImmutableArray<string> RecentlyUnlocked
    )
    {
        public static readonly TechTreeState Empty = new(
            ImmutableDictionary<string, TechNodeStatus>.Empty,
            ImmutableArray<string>.Empty
        );
    }

    /// <summary>
    /// Progression status of a tech tree node. Phase 1+.
    /// </summary>
    public enum TechNodeStatus
    {
        Locked, Available, Researching, Unlocked
    }
}
