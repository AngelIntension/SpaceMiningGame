using NUnit.Framework;
using VoidHarvest.Core.Extensions;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class MiningReducerTests
    {
        private MiningSessionState _emptyState;

        [SetUp]
        public void SetUp()
        {
            _emptyState = MiningSessionState.Empty;
        }

        // --- BeginMiningAction tests ---

        [Test]
        public void BeginMiningAction_SetsTargetAsteroidId()
        {
            var action = new BeginMiningAction(42, "iron_ore");

            var result = MiningReducer.Reduce(_emptyState, action);

            Assert.IsTrue(result.TargetAsteroidId.HasValue);
            Assert.AreEqual(42, result.TargetAsteroidId.GetValueOrDefault(-1));
        }

        [Test]
        public void BeginMiningAction_SetsActiveOreId()
        {
            var action = new BeginMiningAction(42, "iron_ore");

            var result = MiningReducer.Reduce(_emptyState, action);

            Assert.IsTrue(result.ActiveOreId.HasValue);
            Assert.AreEqual("iron_ore", result.ActiveOreId.GetValueOrDefault(""));
        }

        [Test]
        public void BeginMiningAction_ResetsYieldAccumulator()
        {
            var state = _emptyState with { YieldAccumulator = 5.5f };
            var action = new BeginMiningAction(42, "iron_ore");

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0f, result.YieldAccumulator, 0.001f);
        }

        [Test]
        public void BeginMiningAction_SetsBeamEnergyToOne()
        {
            var action = new BeginMiningAction(42, "iron_ore");

            var result = MiningReducer.Reduce(_emptyState, action);

            Assert.AreEqual(1f, result.BeamEnergy, 0.001f);
        }

        [Test]
        public void BeginMiningAction_WithNewTarget_OverwritesPreviousSession()
        {
            var firstAction = new BeginMiningAction(10, "iron_ore");
            var state = MiningReducer.Reduce(_emptyState, firstAction);

            // Simulate some accumulated state from mining the first target
            state = state with { YieldAccumulator = 3.7f, MiningDuration = 10f };

            var secondAction = new BeginMiningAction(20, "gold_ore");
            var result = MiningReducer.Reduce(state, secondAction);

            Assert.AreEqual(20, result.TargetAsteroidId.GetValueOrDefault(-1));
            Assert.AreEqual("gold_ore", result.ActiveOreId.GetValueOrDefault(""));
            Assert.AreEqual(0f, result.YieldAccumulator, 0.001f);
            Assert.AreEqual(1f, result.BeamEnergy, 0.001f);
        }

        // --- MiningTickAction tests ---

        [Test]
        public void MiningTickAction_IncreasesYieldAccumulator()
        {
            // First begin mining so we have an active session
            var state = MiningReducer.Reduce(_emptyState, new BeginMiningAction(1, "iron_ore"));

            // Yield formula: (miningPower * baseYield * deltaTime) / (hardness * (1 + depth))
            // (10 * 2 * 0.5) / (1 * (1 + 0)) = 10 / 1 = 10
            var tick = new MiningTickAction(
                DeltaTime: 0.5f,
                BaseYield: 2f,
                Hardness: 1f,
                Depth: 0f,
                ShipMiningPower: 10f
            );

            var result = MiningReducer.Reduce(state, tick);

            Assert.AreEqual(10f, result.YieldAccumulator, 0.001f);
        }

        [Test]
        public void MiningTickAction_AdvancesMiningDurationByDeltaTime()
        {
            var state = MiningReducer.Reduce(_emptyState, new BeginMiningAction(1, "iron_ore"));

            var tick = new MiningTickAction(
                DeltaTime: 0.25f,
                BaseYield: 1f,
                Hardness: 1f,
                Depth: 0f,
                ShipMiningPower: 1f
            );

            var result = MiningReducer.Reduce(state, tick);

            Assert.AreEqual(0.25f, result.MiningDuration, 0.001f);
        }

        [Test]
        public void MultipleMiningTicks_AccumulateYieldCorrectly()
        {
            var state = MiningReducer.Reduce(_emptyState, new BeginMiningAction(1, "iron_ore"));

            // Yield per tick: (5 * 4 * 1) / (2 * (1 + 0.5)) = 20 / 3 = 6.6667
            var tick = new MiningTickAction(
                DeltaTime: 1f,
                BaseYield: 4f,
                Hardness: 2f,
                Depth: 0.5f,
                ShipMiningPower: 5f
            );

            state = MiningReducer.Reduce(state, tick);
            state = MiningReducer.Reduce(state, tick);
            state = MiningReducer.Reduce(state, tick);

            float expectedPerTick = (5f * 4f * 1f) / (2f * (1f + 0.5f));
            float expectedTotal = expectedPerTick * 3f;

            Assert.AreEqual(expectedTotal, state.YieldAccumulator, 0.001f);
            Assert.AreEqual(3f, state.MiningDuration, 0.001f);
        }

        // --- StopMiningAction tests ---

        [Test]
        public void StopMiningAction_ResetsStateToEmpty()
        {
            var state = MiningReducer.Reduce(_emptyState, new BeginMiningAction(42, "iron_ore"));
            state = MiningReducer.Reduce(state, new MiningTickAction(1f, 2f, 1f, 0f, 10f));

            var result = MiningReducer.Reduce(state, new StopMiningAction());

            Assert.AreEqual(MiningSessionState.Empty, result);
        }

        // --- Unknown action tests ---

        [Test]
        public void UnknownAction_ReturnsUnchangedState()
        {
            var result = MiningReducer.Reduce(_emptyState, new UnknownMiningAction());

            Assert.AreSame(_emptyState, result);
        }

        // --- CalculateYield tests ---

        [Test]
        public void CalculateYield_ReturnsCorrectWholeUnitsAndFraction()
        {
            // (10 * 5 * 1) / (2 * (1 + 0)) = 50 / 2 = 25.0
            var result = MiningReducer.CalculateYield(
                oreId: "iron_ore",
                miningPower: 10f,
                baseYield: 5f,
                hardness: 2f,
                depth: 0f,
                deltaTime: 1f
            );

            Assert.AreEqual("iron_ore", result.OreId);
            Assert.AreEqual(25, result.WholeUnitsYielded);
            Assert.AreEqual(0f, result.RemainingFraction, 0.001f);
        }

        [Test]
        public void CalculateYield_FractionalResult_SplitsCorrectly()
        {
            // (3 * 2 * 1) / (1 * (1 + 1)) = 6 / 2 = 3.0
            // Now use depth 0.5: (3 * 2 * 1) / (1 * (1 + 0.5)) = 6 / 1.5 = 4.0
            // Use values that produce a fractional result:
            // (1 * 1 * 1) / (3 * (1 + 0)) = 1 / 3 = 0.3333...
            var result = MiningReducer.CalculateYield(
                oreId: "gold_ore",
                miningPower: 1f,
                baseYield: 1f,
                hardness: 3f,
                depth: 0f,
                deltaTime: 1f
            );

            Assert.AreEqual("gold_ore", result.OreId);
            Assert.AreEqual(0, result.WholeUnitsYielded);
            Assert.AreEqual(1f / 3f, result.RemainingFraction, 0.001f);
        }

        [Test]
        public void CalculateYield_HighDepth_ReducesYield()
        {
            // (10 * 5 * 1) / (1 * (1 + 9)) = 50 / 10 = 5.0
            var result = MiningReducer.CalculateYield(
                oreId: "iron_ore",
                miningPower: 10f,
                baseYield: 5f,
                hardness: 1f,
                depth: 9f,
                deltaTime: 1f
            );

            Assert.AreEqual(5, result.WholeUnitsYielded);
            Assert.AreEqual(0f, result.RemainingFraction, 0.001f);
        }

        [Test]
        public void CalculateYield_LargeYield_FloorsSplitsCorrectly()
        {
            // (7 * 3 * 2) / (1 * (1 + 0)) = 42 / 1 = 42.0
            // Adjust for fractional: (7 * 3 * 2) / (4 * (1 + 0)) = 42 / 4 = 10.5
            var result = MiningReducer.CalculateYield(
                oreId: "crystal",
                miningPower: 7f,
                baseYield: 3f,
                hardness: 4f,
                depth: 0f,
                deltaTime: 2f
            );

            Assert.AreEqual("crystal", result.OreId);
            Assert.AreEqual(10, result.WholeUnitsYielded);
            Assert.AreEqual(0.5f, result.RemainingFraction, 0.001f);
        }

        // --- Dummy action for unknown action test ---

        private sealed record UnknownMiningAction() : IMiningAction;
    }
}
