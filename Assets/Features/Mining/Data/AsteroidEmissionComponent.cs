using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Per-entity emission color for asteroid vein glow.
    /// Driven by AsteroidEmissionSystem based on depletion fraction.
    /// CONSTITUTION DEVIATION: Mutable ECS shell — same justification as AsteroidComponent
    /// (Burst/cache performance for per-frame emission updates).
    /// </summary>
    [MaterialProperty("_EmissionColor")]
    public struct AsteroidEmissionComponent : IComponentData
    {
        public float4 Value;

        /// <summary>
        /// Fade multiplier [0,1]. Ramps to 1 while actively mined, decays to 0 when mining stops.
        /// Prevents asteroids from glowing indefinitely after mining ceases.
        /// </summary>
        public float GlowFade;
    }
}
