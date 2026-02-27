using System.Collections.Immutable;

namespace VoidHarvest.Features.Procedural.Data
{
    /// <summary>
    /// Immutable configuration for asteroid field generation.
    /// See MVP-08: Procedural field <100ms, 60 FPS.
    /// </summary>
    public sealed record AsteroidFieldConfig(
        uint Seed,
        int MaxAsteroids,
        float FieldRadius,
        ImmutableArray<OreDistribution> OreDistributions
    )
    {
        public static readonly AsteroidFieldConfig MvpDefault = new(
            42,
            300,
            2000f,
            ImmutableArray.Create(
                new OreDistribution("veldspar", 0.6f),
                new OreDistribution("scordite", 0.3f),
                new OreDistribution("pyroxeres", 0.1f)
            )
        );
    }

    /// <summary>
    /// Ore type weight for field generation distribution.
    /// </summary>
    public readonly struct OreDistribution
    {
        /// <summary>Ore type identifier matching OreTypeDefinition.OreId. See MVP-07.</summary>
        public readonly string OreId;
        /// <summary>Relative spawn weight for this ore type in the field. See MVP-07.</summary>
        public readonly float Weight;

        /// <summary>
        /// Create an ore distribution entry. See MVP-07: Procedural asteroid field.
        /// </summary>
        public OreDistribution(string oreId, float weight)
        {
            OreId = oreId;
            Weight = weight;
        }
    }
}
