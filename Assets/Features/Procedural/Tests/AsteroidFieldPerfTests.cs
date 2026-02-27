using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Diagnostics;

namespace VoidHarvest.Features.Procedural.Tests
{
    [TestFixture]
    public class AsteroidFieldPerfTests
    {
        [Test]
        public void GenerateField_500Asteroids_Under100ms()
        {
            var positions = new NativeArray<float3>(500, Allocator.TempJob);
            var oreTypes = new NativeArray<int>(500, Allocator.TempJob);
            var oreWeights = new NativeArray<float>(3, Allocator.TempJob);
            oreWeights[0] = 0.6f;
            oreWeights[1] = 0.3f;
            oreWeights[2] = 0.1f;

            var sw = Stopwatch.StartNew();

            var job = new VoidHarvest.Features.Procedural.Systems.AsteroidFieldGeneratorJob
            {
                Seed = 42,
                MaxAsteroids = 500,
                FieldRadius = 2000f,
                Positions = positions,
                OreTypeIds = oreTypes,
                OreWeights = oreWeights
            };
            job.Schedule(500, 64).Complete();

            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 100, "Field generation should complete in <100ms");

            positions.Dispose();
            oreTypes.Dispose();
            oreWeights.Dispose();
        }
    }
}
