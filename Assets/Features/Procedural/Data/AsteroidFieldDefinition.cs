using UnityEngine;

namespace VoidHarvest.Features.Procedural.Data
{
    /// <summary>
    /// Defines a complete asteroid field configuration. Contains ore composition,
    /// spatial parameters, and visual mapping.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Asteroid Field Definition")]
    public class AsteroidFieldDefinition : ScriptableObject
    {
        /// <summary>Human-readable field name.</summary>
        public string FieldName;

        /// <summary>Ore types with weights and visual mappings.</summary>
        public OreFieldEntry[] OreEntries;

        /// <summary>Number of asteroids to spawn.</summary>
        public int AsteroidCount;

        /// <summary>Spherical field radius in meters.</summary>
        public float FieldRadius;

        /// <summary>Minimum asteroid radius.</summary>
        public float AsteroidSizeMin;

        /// <summary>Maximum asteroid radius.</summary>
        public float AsteroidSizeMax;

        /// <summary>Minimum rotation speed (deg/s).</summary>
        public float RotationSpeedMin;

        /// <summary>Maximum rotation speed (deg/s).</summary>
        public float RotationSpeedMax;

        /// <summary>Deterministic RNG seed.</summary>
        public uint Seed;

        /// <summary>Minimum asteroid scale at full depletion.</summary>
        [Range(0.1f, 0.5f)]
        public float MinScaleFraction = 0.3f;

        /// <summary>
        /// Pure function: normalizes ore entry weights to probabilities.
        /// Entries with null OreDefinition or Weight &lt;= 0 get 0 normalized weight.
        /// Returns empty array if no valid entries exist.
        /// </summary>
        public static float[] NormalizeWeights(OreFieldEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                return System.Array.Empty<float>();

            float totalWeight = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].OreDefinition != null && entries[i].Weight > 0f)
                    totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0f)
                return System.Array.Empty<float>();

            var result = new float[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].OreDefinition != null && entries[i].Weight > 0f)
                    result[i] = entries[i].Weight / totalWeight;
                else
                    result[i] = 0f;
            }

            return result;
        }
    }
}
