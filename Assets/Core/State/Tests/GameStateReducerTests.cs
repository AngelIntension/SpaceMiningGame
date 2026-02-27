using System.Collections.Immutable;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;

namespace VoidHarvest.Core.State.Tests
{
    /// <summary>
    /// Tests for stub reducers (Fleet, TechTree, Market, Base).
    /// Real reducer routing is tested via RootLifetimeScope.CompositeReducer.
    /// </summary>
    [TestFixture]
    public class StubReducerTests
    {
        [Test]
        public void FleetReducer_ReturnsUnchangedState()
        {
            var state = FleetState.Empty;
            var result = FleetReducer.Reduce(state, null);
            Assert.AreSame(state, result);
        }

        [Test]
        public void TechTreeReducer_ReturnsUnchangedState()
        {
            var state = TechTreeState.Empty;
            var result = TechTreeReducer.Reduce(state, null);
            Assert.AreSame(state, result);
        }

        [Test]
        public void MarketReducer_ReturnsUnchangedState()
        {
            var state = MarketState.Empty;
            var result = MarketReducer.Reduce(state, null);
            Assert.AreSame(state, result);
        }

        [Test]
        public void BaseReducer_ReturnsUnchangedState()
        {
            var state = BaseState.Empty;
            var result = BaseReducer.Reduce(state, null);
            Assert.AreSame(state, result);
        }
    }
}
