using System.Collections.Immutable;
using Unity.Mathematics;

namespace VoidHarvest.Core.State
{
    /// <summary>
    /// Base building state. Stub — Phase 2+.
    /// </summary>
    public sealed record BaseState(
        ImmutableArray<PlacedModule> Modules
    )
    {
        public static readonly BaseState Empty = new(ImmutableArray<PlacedModule>.Empty);
    }

    /// <summary>
    /// A module placed in the player's base. Phase 2+.
    /// </summary>
    public sealed record PlacedModule(
        string ModuleId,
        float3 Position,
        quaternion Rotation,
        string ModuleTypeId
    );
}
