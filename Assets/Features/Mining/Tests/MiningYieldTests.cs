using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class MiningYieldTests
    {
        // Formula: amount = (miningPower * baseYield * deltaTime) / (hardness * (1 + depth))

        [Test]
        public void StandardCalculation_KnownValues_ReturnsExactResult()
        {
            // miningPower=1, baseYield=10, hardness=1, depth=0, deltaTime=1
            // amount = (1 * 10 * 1) / (1 * (1 + 0)) = 10
            var result = MiningReducer.CalculateYield("ore_iron", 1f, 10f, 1f, 0f, 1f);

            Assert.AreEqual("ore_iron", result.OreId);
            Assert.AreEqual(10, result.WholeUnitsYielded);
            Assert.AreEqual(0f, result.RemainingFraction, 0.0001f);
        }

        [Test]
        public void HardnessReducesYield_HigherHardness_LowerYield()
        {
            var lowHardness = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 1f, 0f, 1f);
            var highHardness = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 5f, 0f, 1f);

            float lowTotal = lowHardness.WholeUnitsYielded + lowHardness.RemainingFraction;
            float highTotal = highHardness.WholeUnitsYielded + highHardness.RemainingFraction;

            Assert.Greater(lowTotal, highTotal,
                "Higher hardness should produce lower total yield");
        }

        [Test]
        public void DepthReducesYield_HigherDepth_LowerYield()
        {
            var shallow = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 1f, 0f, 1f);
            var deep = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 1f, 5f, 1f);

            float shallowTotal = shallow.WholeUnitsYielded + shallow.RemainingFraction;
            float deepTotal = deep.WholeUnitsYielded + deep.RemainingFraction;

            Assert.Greater(shallowTotal, deepTotal,
                "Higher depth should produce lower total yield");
        }

        [Test]
        public void MiningPowerScalesLinearly_DoublingPower_DoublesYield()
        {
            var single = MiningReducer.CalculateYield("ore_iron", 5f, 10f, 2f, 1f, 1f);
            var doubled = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 2f, 1f, 1f);

            float singleTotal = single.WholeUnitsYielded + single.RemainingFraction;
            float doubledTotal = doubled.WholeUnitsYielded + doubled.RemainingFraction;

            Assert.AreEqual(singleTotal * 2f, doubledTotal, 0.0001f,
                "Doubling mining power should double the total yield");
        }

        [Test]
        public void DeltaTimeScalesOutput_DoublingDeltaTime_DoublesYield()
        {
            var singleDt = MiningReducer.CalculateYield("ore_iron", 5f, 10f, 2f, 1f, 0.5f);
            var doubleDt = MiningReducer.CalculateYield("ore_iron", 5f, 10f, 2f, 1f, 1.0f);

            float singleTotal = singleDt.WholeUnitsYielded + singleDt.RemainingFraction;
            float doubleTotal = doubleDt.WholeUnitsYielded + doubleDt.RemainingFraction;

            Assert.AreEqual(singleTotal * 2f, doubleTotal, 0.0001f,
                "Doubling deltaTime should double the total yield");
        }

        [Test]
        public void ZeroHardnessGuard_ReturnsZeroYield()
        {
            var result = MiningReducer.CalculateYield("ore_iron", 10f, 10f, 0f, 0f, 1f);

            Assert.AreEqual(0, result.WholeUnitsYielded,
                "Zero hardness should return zero WholeUnitsYielded");
            Assert.AreEqual(0f, result.RemainingFraction, 0.0001f,
                "Zero hardness should return zero RemainingFraction");
        }

        [Test]
        public void FractionalYield_ProducesCorrectWholeAndFraction()
        {
            // miningPower=5, baseYield=1, hardness=1, depth=1, deltaTime=1
            // amount = (5 * 1 * 1) / (1 * (1 + 1)) = 5 / 2 = 2.5
            var result = MiningReducer.CalculateYield("ore_gold", 5f, 1f, 1f, 1f, 1f);

            Assert.AreEqual(2, result.WholeUnitsYielded);
            Assert.AreEqual(0.5f, result.RemainingFraction, 0.0001f);
        }

        [Test]
        public void OreIdPassthrough_ReturnedOreIdMatchesInput()
        {
            const string expectedOreId = "ore_luminite";
            var result = MiningReducer.CalculateYield(expectedOreId, 1f, 1f, 1f, 0f, 1f);

            Assert.AreEqual(expectedOreId, result.OreId,
                "Returned OreId must match the input oreId");
        }

        [Test]
        public void VerySmallYield_TinyDeltaTime_ZeroWholeUnitsWithNonZeroFraction()
        {
            // miningPower=1, baseYield=1, hardness=1, depth=0, deltaTime=0.001
            // amount = (1 * 1 * 0.001) / (1 * 1) = 0.001
            var result = MiningReducer.CalculateYield("ore_iron", 1f, 1f, 1f, 0f, 0.001f);

            Assert.AreEqual(0, result.WholeUnitsYielded,
                "Tiny deltaTime should produce zero whole units");
            Assert.Greater(result.RemainingFraction, 0f,
                "Tiny deltaTime should still produce a non-zero remaining fraction");
            Assert.AreEqual(0.001f, result.RemainingFraction, 0.0001f);
        }
    }
}
