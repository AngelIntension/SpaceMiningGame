using UnityEngine;

namespace VoidHarvest.Features.Procedural.Data
{
    /// <summary>
    /// Defines a complete asteroid field configuration. Contains ore composition,
    /// spatial parameters, and visual mapping.
    /// See Spec 005: Data-Driven Ore System.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidHarvest/Procedural/Asteroid Field Definition")]
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

        private void OnValidate()
        {
            if (AsteroidCount <= 0)
                Debug.LogWarning($"[{name}] AsteroidCount must be > 0");
            if (FieldRadius <= 0f)
                Debug.LogWarning($"[{name}] FieldRadius must be > 0");
            if (AsteroidSizeMin <= 0f)
                Debug.LogWarning($"[{name}] AsteroidSizeMin must be > 0");
            if (AsteroidSizeMax < AsteroidSizeMin)
                Debug.LogWarning($"[{name}] AsteroidSizeMax must be >= AsteroidSizeMin");
            if (RotationSpeedMax < RotationSpeedMin)
                Debug.LogWarning($"[{name}] RotationSpeedMax must be >= RotationSpeedMin");
            if (MinScaleFraction < 0.1f || MinScaleFraction > 0.5f)
                Debug.LogWarning($"[{name}] MinScaleFraction must be in [0.1, 0.5]");
            if (OreEntries == null || OreEntries.Length == 0)
            {
                Debug.LogWarning($"[{name}] OreEntries must have at least one entry");
            }
            else
            {
                for (int i = 0; i < OreEntries.Length; i++)
                {
                    if (OreEntries[i].OreDefinition == null)
                        Debug.LogWarning($"[{name}] OreEntries[{i}].OreDefinition must not be null");
                    if (OreEntries[i].Weight <= 0f)
                        Debug.LogWarning($"[{name}] OreEntries[{i}].Weight must be > 0");
                }
            }
        }
    }
}
