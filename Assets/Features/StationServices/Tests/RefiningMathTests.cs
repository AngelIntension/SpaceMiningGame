using System.Collections.Immutable;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class RefiningMathTests
    {
        [Test]
        public void CalculateOutputs_KnownSeed_DeterministicYields()
        {
            var configs = ImmutableArray.Create(
                new RefiningOutputConfig("luminite_ingots", 4, -1, 2)
            );
            var random1 = new Random(42u);
            var result1 = RefiningMath.CalculateOutputs(configs, 5, ref random1);

            var random2 = new Random(42u);
            var result2 = RefiningMath.CalculateOutputs(configs, 5, ref random2);

            Assert.AreEqual(result1[0].Quantity, result2[0].Quantity, "Same seed must produce same output");
        }

        [Test]
        public void CalculateOutputs_PerUnitRolling_10Units()
        {
            var configs = ImmutableArray.Create(
                new RefiningOutputConfig("luminite_ingots", 4, 0, 0) // No variance, exactly 4 per unit
            );
            var random = new Random(1u);
            var result = RefiningMath.CalculateOutputs(configs, 10, ref random);

            Assert.AreEqual(40, result[0].Quantity, "10 units × 4 base yield = 40");
        }

        [Test]
        public void CalculateOutputs_FloorAtZero_WhenNegative()
        {
            var configs = ImmutableArray.Create(
                new RefiningOutputConfig("test_mat", 1, -5, -5) // base 1 + offset -5 = -4, floored to 0
            );
            var random = new Random(99u);
            var result = RefiningMath.CalculateOutputs(configs, 3, ref random);

            Assert.AreEqual(0, result[0].Quantity, "Negative yields should be floored at 0");
        }

        [Test]
        public void CalculateOutputs_ZeroInput_ProducesZero()
        {
            var configs = ImmutableArray.Create(
                new RefiningOutputConfig("luminite_ingots", 4, -1, 2)
            );
            var random = new Random(1u);
            var result = RefiningMath.CalculateOutputs(configs, 0, ref random);

            Assert.AreEqual(0, result[0].Quantity);
        }

        [Test]
        public void CalculateOutputs_MultipleConfigs_IndependentCalculation()
        {
            var configs = ImmutableArray.Create(
                new RefiningOutputConfig("mat_a", 4, 0, 0),
                new RefiningOutputConfig("mat_b", 2, 0, 0)
            );
            var random = new Random(1u);
            var result = RefiningMath.CalculateOutputs(configs, 5, ref random);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(20, result[0].Quantity, "mat_a: 5 × 4 = 20");
            Assert.AreEqual(10, result[1].Quantity, "mat_b: 5 × 2 = 10");
        }

        [Test]
        public void CalculateJobDuration_WithSpeedMultiplier()
        {
            float duration = RefiningMath.CalculateJobDuration(10, 5f, 2f);
            Assert.AreEqual(25f, duration, 0.001f, "(10 × 5) / 2 = 25");
        }

        [Test]
        public void CalculateJobDuration_MinSpeedMultiplier()
        {
            float duration = RefiningMath.CalculateJobDuration(10, 5f, 0f);
            // Should use min 0.01, so (10 × 5) / 0.01 = 5000
            Assert.AreEqual(5000f, duration, 0.1f);
        }

        [Test]
        public void CalculateJobCost_ReturnsInt()
        {
            int cost = RefiningMath.CalculateJobCost(10, 5);
            Assert.AreEqual(50, cost);
        }
    }
}
