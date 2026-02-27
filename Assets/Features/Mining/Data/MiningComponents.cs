using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace VoidHarvest.Features.Mining.Data
{
    /// <summary>
    /// Per-entity material property override for asteroid base color.
    /// Entities Graphics reads this and applies it to the URP Lit shader _BaseColor.
    /// See MVP-07: Asteroid depletion visual.
    /// </summary>
    [MaterialProperty("_BaseColor")]
    public struct AsteroidBaseColorOverride : IComponentData
    {
        /// <summary>RGBA color value. See MVP-07.</summary>
        public float4 Value;
    }

    /// <summary>
    /// Mining beam component on the ship entity. Set Active=true when mining starts.
    /// Baked with Active=false in ShipBaker (T040).
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public struct MiningBeamComponent : IComponentData
    {
        /// <summary>Entity reference to the asteroid being mined. See MVP-05.</summary>
        public Entity TargetAsteroid;
        /// <summary>Current beam energy level [0, 1]. See MVP-05.</summary>
        public float BeamEnergy;
        /// <summary>Yield multiplier from ship archetype. See MVP-05.</summary>
        public float MiningPower;
        /// <summary>Maximum beam range in meters. See MVP-05.</summary>
        public float MaxRange;
        /// <summary>Whether the mining beam is currently firing. See MVP-05.</summary>
        public bool Active;
    }

    /// <summary>
    /// Asteroid entity data. Set during baking from AsteroidFieldGeneratorJob.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// See MVP-07: Asteroid depletion visual.
    /// </summary>
    public struct AsteroidComponent : IComponentData
    {
        /// <summary>Asteroid radius in meters, used for collision and rendering scale. See MVP-07.</summary>
        public float Radius;
        /// <summary>Original mass at spawn time. See MVP-07.</summary>
        public float InitialMass;
        /// <summary>Current mass after mining extraction. See MVP-05.</summary>
        public float RemainingMass;
        /// <summary>Depletion fraction [0, 1] driving shader _Depletion parameter. See MVP-07.</summary>
        public float Depletion;
    }

    /// <summary>
    /// Ore type data on an asteroid entity. OreTypeId is index into OreTypeBlobDatabase.
    /// // CONSTITUTION DEVIATION: ECS mutable shell
    /// See MVP-05: Mining beam and yield.
    /// </summary>
    public struct AsteroidOreComponent : IComponentData
    {
        /// <summary>Index into OreTypeBlobDatabase. See MVP-05.</summary>
        public int OreTypeId;
        /// <summary>Remaining ore quantity for this asteroid. See MVP-05.</summary>
        public float Quantity;
        /// <summary>Mining depth factor increasing extraction difficulty. See MVP-05.</summary>
        public float Depth;
    }
}
