using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoidHarvest.Features.Procedural.Systems
{
    /// <summary>
    /// Burst-compiled parallel job for generating asteroid positions and ore assignments.
    /// Uses seeded RNG for deterministic output. Same seed = identical field.
    /// See MVP-08: Procedural field generation <100ms.
    /// </summary>
    [BurstCompile]
    public struct AsteroidFieldGeneratorJob : IJobParallelFor
    {
        /// <summary>RNG seed for deterministic field generation. See MVP-07.</summary>
        public uint Seed;
        /// <summary>Total number of asteroids to generate. See MVP-07.</summary>
        public int MaxAsteroids;
        /// <summary>Spherical field radius in meters. See MVP-07.</summary>
        public float FieldRadius;

        /// <summary>Output: world-space positions for each asteroid. See MVP-07.</summary>
        [WriteOnly] public NativeArray<float3> Positions;
        /// <summary>Output: ore type index for each asteroid. See MVP-07.</summary>
        [WriteOnly] public NativeArray<int> OreTypeIds;
        /// <summary>Input: ore type distribution weights for weighted random selection. See MVP-07.</summary>
        [ReadOnly] public NativeArray<float> OreWeights;

        /// <summary>
        /// Generate position and ore type for a single asteroid. See MVP-07: Procedural asteroid field.
        /// </summary>
        public void Execute(int index)
        {
            // Per-index seeded RNG for determinism
            var rng = new Random(Seed + (uint)index + 1u);

            if (FieldRadius <= 0f)
            {
                Positions[index] = float3.zero;
                OreTypeIds[index] = 0;
                return;
            }

            // Spherical distribution within field radius
            float r = math.pow(rng.NextFloat(), 1f / 3f) * FieldRadius;
            float theta = rng.NextFloat() * math.PI * 2f;
            float phi = math.acos(2f * rng.NextFloat() - 1f);

            float x = r * math.sin(phi) * math.cos(theta);
            float y = r * math.sin(phi) * math.sin(theta);
            float z = r * math.cos(phi);

            Positions[index] = new float3(x, y, z);

            // Ore assignment via weighted random
            float roll = rng.NextFloat();
            float cumulative = 0f;
            int oreType = 0;

            for (int i = 0; i < OreWeights.Length; i++)
            {
                cumulative += OreWeights[i];
                if (roll <= cumulative)
                {
                    oreType = i;
                    break;
                }
            }

            OreTypeIds[index] = oreType;
        }
    }
}
