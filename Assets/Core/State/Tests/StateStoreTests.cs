using System.Collections.Immutable;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;

namespace VoidHarvest.Core.State.Tests
{
    [TestFixture]
    public class StateStoreTests
    {
        private StateStore _store;
        private UniTaskEventBus _eventBus;
        private GameState _initialState;

        private sealed record TestAction(int Value) : IGameAction;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new UniTaskEventBus();
            _initialState = CreateDefaultGameState();

            _store = new StateStore(
                (state, action) =>
                {
                    if (action is TestAction ta)
                        return state with { Camera = state.Camera with { OrbitYaw = ta.Value } };
                    return state;
                },
                _initialState,
                _eventBus
            );
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
            _eventBus.Dispose();
        }

        [Test]
        public void Current_AfterInit_ReturnsInitialState()
        {
            Assert.AreSame(_initialState, _store.Current);
        }

        [Test]
        public void Version_AfterInit_IsZero()
        {
            Assert.AreEqual(0, _store.Version);
        }

        [Test]
        public void Dispatch_ProducesNewState()
        {
            _store.Dispatch(new TestAction(45));
            Assert.AreEqual(45f, _store.Current.Camera.OrbitYaw);
        }

        [Test]
        public void Dispatch_IncrementsVersion()
        {
            _store.Dispatch(new TestAction(1));
            Assert.AreEqual(1, _store.Version);

            _store.Dispatch(new TestAction(2));
            Assert.AreEqual(2, _store.Version);
        }

        [Test]
        public void Dispatch_NullAction_IsNoOp()
        {
            _store.Dispatch(null);
            Assert.AreEqual(0, _store.Version);
            Assert.AreSame(_initialState, _store.Current);
        }

        [Test]
        public void Dispatch_UnknownAction_ReturnsUnchangedState()
        {
            var before = _store.Current;
            _store.Dispatch(new UnknownAction());
            Assert.AreEqual(1, _store.Version); // Version still increments
        }

        [Test]
        public void MultipleDispatches_VersionIncrementsCorrectly()
        {
            for (int i = 0; i < 10; i++)
                _store.Dispatch(new TestAction(i));

            Assert.AreEqual(10, _store.Version);
            Assert.AreEqual(9f, _store.Current.Camera.OrbitYaw);
        }

        private sealed record UnknownAction() : IGameAction;

        private static GameState CreateDefaultGameState()
        {
            return new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    RefiningState.Empty,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty
                ),
                ActiveShipPhysics: ShipState.Default,
                Camera: CameraState.Default,
                World: new WorldState(
                    ImmutableArray<StationData>.Empty,
                    0f
                )
            );
        }
    }
}
