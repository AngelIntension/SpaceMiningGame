using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class MiningDepletionReducerTests
    {
        [Test]
        public void MiningDepletionTickAction_UpdatesDepletionFraction()
        {
            var state = MiningSessionState.Empty with { DepletionFraction = 0f };
            var action = new MiningDepletionTickAction(0.5f);

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0.5f, result.DepletionFraction, 0.001f);
        }

        [Test]
        public void MiningDepletionTickAction_AtZero_RemainsZero()
        {
            var state = MiningSessionState.Empty with { DepletionFraction = 0f };
            var action = new MiningDepletionTickAction(0f);

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0f, result.DepletionFraction, 0.001f);
        }

        [Test]
        public void MiningDepletionTickAction_AtHalf_SetsHalf()
        {
            var state = MiningSessionState.Empty;
            var action = new MiningDepletionTickAction(0.5f);

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0.5f, result.DepletionFraction, 0.001f);
        }

        [Test]
        public void MiningDepletionTickAction_AtFull_SetsFull()
        {
            var state = MiningSessionState.Empty;
            var action = new MiningDepletionTickAction(1.0f);

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(1.0f, result.DepletionFraction, 0.001f);
        }

        [Test]
        public void MiningDepletionTickAction_PreservesOtherFields()
        {
            var state = MiningSessionState.Empty with
            {
                BeamEnergy = 0.8f,
                YieldAccumulator = 5.5f,
                MiningDuration = 10f
            };
            var action = new MiningDepletionTickAction(0.75f);

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0.75f, result.DepletionFraction, 0.001f);
            Assert.AreEqual(0.8f, result.BeamEnergy, 0.001f);
            Assert.AreEqual(5.5f, result.YieldAccumulator, 0.001f);
            Assert.AreEqual(10f, result.MiningDuration, 0.001f);
        }

        [Test]
        public void BeginMiningAction_ResetsDepletionFraction()
        {
            var state = MiningSessionState.Empty with { DepletionFraction = 0.75f };
            var action = new BeginMiningAction(42, "Veldspar");

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0f, result.DepletionFraction, 0.001f);
        }

        [Test]
        public void StopMiningAction_ResetsDepletionFraction()
        {
            var state = MiningSessionState.Empty with { DepletionFraction = 0.5f };
            var action = new StopMiningAction();

            var result = MiningReducer.Reduce(state, action);

            Assert.AreEqual(0f, result.DepletionFraction, 0.001f);
        }
    }
}
