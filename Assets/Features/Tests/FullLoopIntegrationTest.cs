using System.Collections.Immutable;
using System.Diagnostics;
using NUnit.Framework;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Camera.Systems;
using VoidHarvest.Features.Mining.Systems;
using VoidHarvest.Features.Resources.Systems;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Tests
{
    /// <summary>
    /// Full-loop integration test: exercises the complete reducer pipeline
    /// from target selection through mining to inventory update.
    /// See T074: full loop integration test.
    /// </summary>
    [TestFixture]
    public class FullLoopIntegrationTest
    {
        private StateStore _store;
        private UniTaskEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new UniTaskEventBus();
            var initialState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    RefiningState.Empty,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty,
                    DockingState.Empty
                ),
                ActiveShipPhysics: ShipState.Default,
                Camera: CameraState.Default,
                World: new WorldState(ImmutableArray<StationData>.Empty, 0f)
            );

            _store = new StateStore(CompositeReducer, initialState, _eventBus);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
            _eventBus.Dispose();
        }

        [Test]
        public void FullLoop_SelectTarget_BeginMining_Yield_Inventory()
        {
            // US2: Select target asteroid
            int asteroidId = 42;
            string oreId = "veldspar";

            // US3: Begin mining — reducer sets active session
            _store.Dispatch(new BeginMiningAction(asteroidId, oreId));

            var mining = _store.Current.Loop.Mining;
            Assert.IsTrue(mining.TargetAsteroidId.HasValue);
            Assert.AreEqual(asteroidId, mining.TargetAsteroidId.GetValueOrDefault(-1));
            Assert.AreEqual(oreId, mining.ActiveOreId.GetValueOrDefault(""));
            Assert.AreEqual(1f, mining.BeamEnergy, 0.001f);

            // US3: Mining ticks produce yield
            _store.Dispatch(new MiningTickAction(
                DeltaTime: 0.016f,
                BaseYield: 1f,
                Hardness: 1f,
                Depth: 0f,
                ShipMiningPower: 100f
            ));

            Assert.Greater(_store.Current.Loop.Mining.YieldAccumulator, 0f);

            // US3: Resources added to inventory
            float volumePerUnit = 0.1f;
            _store.Dispatch(new AddResourceAction(oreId, 5, volumePerUnit));

            var inventory = _store.Current.Loop.Inventory;
            Assert.IsTrue(inventory.Stacks.ContainsKey(oreId));
            Assert.AreEqual(5, inventory.Stacks[oreId].Quantity);
            Assert.AreEqual(0.5f, inventory.CurrentVolume, 0.001f);

            // US3: Stop mining — session resets
            _store.Dispatch(new StopMiningAction());

            var stoppedMining = _store.Current.Loop.Mining;
            Assert.AreEqual(MiningSessionState.Empty, stoppedMining);

            // Inventory persists after mining stops
            Assert.AreEqual(5, _store.Current.Loop.Inventory.Stacks[oreId].Quantity);
        }

        [Test]
        public void FullLoop_CargoFull_RejectsAdditionalResources()
        {
            // Fill cargo to near capacity
            var state = _store.Current;
            float volumePerUnit = 10f;
            _store.Dispatch(new AddResourceAction("veldspar", 9, volumePerUnit)); // 90/100

            var prevInventory = _store.Current.Loop.Inventory;

            // Try to add more than remaining capacity
            _store.Dispatch(new AddResourceAction("veldspar", 2, volumePerUnit)); // 90 + 20 = 110 > 100

            // Inventory unchanged — cargo full
            Assert.AreSame(prevInventory, _store.Current.Loop.Inventory);
        }

        [Test]
        public void FullLoop_AsteroidDepleted_StopMiningResetsSession()
        {
            _store.Dispatch(new BeginMiningAction(99, "scordite"));

            // Simulate mining ticks
            for (int i = 0; i < 10; i++)
            {
                _store.Dispatch(new MiningTickAction(0.016f, 1f, 1f, 0f, 50f));
            }

            // Asteroid depleted — stop mining
            _store.Dispatch(new StopMiningAction());

            Assert.AreEqual(MiningSessionState.Empty, _store.Current.Loop.Mining);
        }

        [Test]
        public void ReducerLatency_Under2ms_PerDispatch()
        {
            // Warm up
            _store.Dispatch(new BeginMiningAction(1, "veldspar"));
            _store.Dispatch(new StopMiningAction());

            var sw = Stopwatch.StartNew();
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                _store.Dispatch(new BeginMiningAction(i, "veldspar"));
                _store.Dispatch(new MiningTickAction(0.016f, 1f, 1f, 0f, 10f));
                _store.Dispatch(new AddResourceAction("veldspar", 1, 0.1f));
                _store.Dispatch(new StopMiningAction());
            }

            sw.Stop();
            double avgMs = sw.Elapsed.TotalMilliseconds / (iterations * 4);

            Assert.Less(avgMs, 2.0, $"Average reducer dispatch latency {avgMs:F4}ms exceeds 2ms target");
        }

        /// <summary>
        /// Local composite reducer routing actions to feature reducers.
        /// Mirrors RootLifetimeScope.CompositeReducer without Assembly-CSharp dependency.
        /// </summary>
        private static GameState CompositeReducer(GameState state, IGameAction action)
            => action switch
            {
                ICameraAction a    => state with { Camera = CameraReducer.Reduce(state.Camera, a) },
                IShipAction a      => state with { ActiveShipPhysics = ShipStateReducer.Reduce(state.ActiveShipPhysics, a) },
                IMiningAction a    => state with { Loop = state.Loop with { Mining = MiningReducer.Reduce(state.Loop.Mining, a) } },
                IInventoryAction a => state with { Loop = state.Loop with { Inventory = InventoryReducer.Reduce(state.Loop.Inventory, a) } },
                _ => state
            };
    }
}
