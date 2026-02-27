using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using VoidHarvest.Features.Procedural.Systems;

namespace VoidHarvest.Features.Procedural.Tests
{
    [TestFixture]
    public class AsteroidFieldGeneratorTests
    {
        private const uint TestSeed = 42;
        private const float TestFieldRadius = 2000f;

        private NativeArray<float> _oreWeights;

        [SetUp]
        public void SetUp()
        {
            _oreWeights = new NativeArray<float>(3, Allocator.TempJob);
            _oreWeights[0] = 0.6f;
            _oreWeights[1] = 0.3f;
            _oreWeights[2] = 0.1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_oreWeights.IsCreated) _oreWeights.Dispose();
        }

        [Test]
        public void SameSeed_ProducesIdenticalPositions()
        {
            const int count = 100;

            var positionsA = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesA = new NativeArray<int>(count, Allocator.TempJob);
            var positionsB = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesB = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var jobA = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsA,
                    OreTypeIds = oreTypesA,
                    OreWeights = _oreWeights
                };
                jobA.Schedule(count, 64).Complete();

                var jobB = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsB,
                    OreTypeIds = oreTypesB,
                    OreWeights = _oreWeights
                };
                jobB.Schedule(count, 64).Complete();

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(positionsA[i].x, positionsB[i].x, 0.0001f,
                        $"Position X mismatch at index {i}");
                    Assert.AreEqual(positionsA[i].y, positionsB[i].y, 0.0001f,
                        $"Position Y mismatch at index {i}");
                    Assert.AreEqual(positionsA[i].z, positionsB[i].z, 0.0001f,
                        $"Position Z mismatch at index {i}");
                }
            }
            finally
            {
                positionsA.Dispose();
                oreTypesA.Dispose();
                positionsB.Dispose();
                oreTypesB.Dispose();
            }
        }

        [Test]
        public void SameSeed_ProducesIdenticalOreAssignments()
        {
            const int count = 100;

            var positionsA = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesA = new NativeArray<int>(count, Allocator.TempJob);
            var positionsB = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesB = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var jobA = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsA,
                    OreTypeIds = oreTypesA,
                    OreWeights = _oreWeights
                };
                jobA.Schedule(count, 64).Complete();

                var jobB = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsB,
                    OreTypeIds = oreTypesB,
                    OreWeights = _oreWeights
                };
                jobB.Schedule(count, 64).Complete();

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(oreTypesA[i], oreTypesB[i],
                        $"Ore type mismatch at index {i}");
                }
            }
            finally
            {
                positionsA.Dispose();
                oreTypesA.Dispose();
                positionsB.Dispose();
                oreTypesB.Dispose();
            }
        }

        [Test]
        public void RespectsMaxAsteroidsCap()
        {
            const int maxAsteroids = 50;

            var positions = new NativeArray<float3>(maxAsteroids, Allocator.TempJob);
            var oreTypes = new NativeArray<int>(maxAsteroids, Allocator.TempJob);

            try
            {
                var job = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = maxAsteroids,
                    FieldRadius = TestFieldRadius,
                    Positions = positions,
                    OreTypeIds = oreTypes,
                    OreWeights = _oreWeights
                };
                job.Schedule(maxAsteroids, 64).Complete();

                Assert.AreEqual(maxAsteroids, positions.Length,
                    "Output array length should equal MaxAsteroids");
            }
            finally
            {
                positions.Dispose();
                oreTypes.Dispose();
            }
        }

        [Test]
        public void AllPositionsWithinFieldRadius()
        {
            const int count = 300;

            var positions = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypes = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var job = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positions,
                    OreTypeIds = oreTypes,
                    OreWeights = _oreWeights
                };
                job.Schedule(count, 64).Complete();

                for (int i = 0; i < count; i++)
                {
                    float distance = math.length(positions[i]);
                    Assert.LessOrEqual(distance, TestFieldRadius,
                        $"Asteroid at index {i} is at distance {distance}, exceeding FieldRadius {TestFieldRadius}");
                }
            }
            finally
            {
                positions.Dispose();
                oreTypes.Dispose();
            }
        }

        [Test]
        public void OreDistributionMatchesWeightsWithinTolerance()
        {
            const int count = 3000;
            const float tolerance = 0.05f;

            var positions = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypes = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var job = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positions,
                    OreTypeIds = oreTypes,
                    OreWeights = _oreWeights
                };
                job.Schedule(count, 64).Complete();

                int[] oreCounts = new int[3];
                for (int i = 0; i < count; i++)
                {
                    int oreId = oreTypes[i];
                    Assert.GreaterOrEqual(oreId, 0, $"Ore type at index {i} is negative");
                    Assert.Less(oreId, 3, $"Ore type at index {i} exceeds weight count");
                    oreCounts[oreId]++;
                }

                float[] expectedWeights = { 0.6f, 0.3f, 0.1f };
                for (int w = 0; w < expectedWeights.Length; w++)
                {
                    float actualRatio = (float)oreCounts[w] / count;
                    Assert.AreEqual(expectedWeights[w], actualRatio, tolerance,
                        $"Ore type {w}: expected ratio ~{expectedWeights[w]}, got {actualRatio} ({oreCounts[w]}/{count})");
                }
            }
            finally
            {
                positions.Dispose();
                oreTypes.Dispose();
            }
        }

        [Test]
        public void ZeroRadiusGuard_ProducesValidPositions()
        {
            const int count = 10;
            const float zeroRadius = 0f;

            var positions = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypes = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var job = new AsteroidFieldGeneratorJob
                {
                    Seed = TestSeed,
                    MaxAsteroids = count,
                    FieldRadius = zeroRadius,
                    Positions = positions,
                    OreTypeIds = oreTypes,
                    OreWeights = _oreWeights
                };
                job.Schedule(count, 64).Complete();

                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(0f, positions[i].x, 0.0001f,
                        $"Position X at index {i} should be zero for zero-radius field");
                    Assert.AreEqual(0f, positions[i].y, 0.0001f,
                        $"Position Y at index {i} should be zero for zero-radius field");
                    Assert.AreEqual(0f, positions[i].z, 0.0001f,
                        $"Position Z at index {i} should be zero for zero-radius field");
                }
            }
            finally
            {
                positions.Dispose();
                oreTypes.Dispose();
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentFields()
        {
            const int count = 100;

            var positionsA = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesA = new NativeArray<int>(count, Allocator.TempJob);
            var positionsB = new NativeArray<float3>(count, Allocator.TempJob);
            var oreTypesB = new NativeArray<int>(count, Allocator.TempJob);

            try
            {
                var jobA = new AsteroidFieldGeneratorJob
                {
                    Seed = 42,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsA,
                    OreTypeIds = oreTypesA,
                    OreWeights = _oreWeights
                };
                jobA.Schedule(count, 64).Complete();

                var jobB = new AsteroidFieldGeneratorJob
                {
                    Seed = 99,
                    MaxAsteroids = count,
                    FieldRadius = TestFieldRadius,
                    Positions = positionsB,
                    OreTypeIds = oreTypesB,
                    OreWeights = _oreWeights
                };
                jobB.Schedule(count, 64).Complete();

                bool anyPositionDiffers = false;
                for (int i = 0; i < count; i++)
                {
                    if (math.distance(positionsA[i], positionsB[i]) > 0.0001f)
                    {
                        anyPositionDiffers = true;
                        break;
                    }
                }

                Assert.IsTrue(anyPositionDiffers,
                    "Different seeds should produce different asteroid positions");
            }
            finally
            {
                positionsA.Dispose();
                oreTypesA.Dispose();
                positionsB.Dispose();
                oreTypesB.Dispose();
            }
        }
    }
}
