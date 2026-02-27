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
    /// See MVP-07: Asteroid depletion visual, FR-018 through FR-021: Depletion visuals.
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

        /// <summary>
        /// Ore-tinted pristine color, set at spawn: pristineGray (0.314) * oreTintColor.
        /// Used by AsteroidDepletionSystem as the base color for depletion lerp instead of
        /// hardcoded constant. See FR-008: Ore tint, data-model.md PristineTintedColor.
        /// </summary>
        public float4 PristineTintedColor;

        /// <summary>
        /// Bitmask tracking crossed depletion thresholds: bit0=75% remaining (25% depleted),
        /// bit1=50%, bit2=25%, bit3=0%. Prevents re-triggering already-passed thresholds.
        /// See FR-020: Crumble pauses.
        /// </summary>
        public byte CrumbleThresholdsPassed;

        /// <summary>
        /// Countdown timer for current crumble pause in seconds (0 = no pause active).
        /// Set to CrumblePauseDuration when a threshold is crossed.
        /// See FR-020: Crumble pauses.
        /// </summary>
        public float CrumblePauseTimer;

        /// <summary>
        /// Countdown timer for fade-out after final crumble in seconds.
        /// 0 = no fade active; greater than 0 = fading; less than 0 = ready for removal.
        /// See FR-021: Fade-out removal.
        /// </summary>
        public float FadeOutTimer;
    }

    /// <summary>
    /// Singleton component holding the MinScaleFraction config value for AsteroidScaleSystem.
    /// Set by AsteroidPrefabBaker from AsteroidVisualMappingConfig.
    /// See FR-019: Depletion shrink.
    /// </summary>
    public struct AsteroidVisualMappingSingleton : IComponentData
    {
        /// <summary>Minimum scale multiplier at full depletion (default 0.3, range 0.1–0.5).</summary>
        public float MinScaleFraction;
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
