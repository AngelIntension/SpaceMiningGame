using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class RepairTests
    {
        [Test]
        public void RepairMath_At60Percent_Returns40()
        {
            int cost = RepairMath.CalculateRepairCost(0.6f, 100);
            Assert.AreEqual(40, cost);
        }

        [Test]
        public void RepairMath_At0Percent_Returns100()
        {
            int cost = RepairMath.CalculateRepairCost(0f, 100);
            Assert.AreEqual(100, cost);
        }

        [Test]
        public void RepairMath_At100Percent_Returns0()
        {
            int cost = RepairMath.CalculateRepairCost(1f, 100);
            Assert.AreEqual(0, cost);
        }

        [Test]
        public void RepairMath_At99Point5Percent_CeilingReturns1()
        {
            int cost = RepairMath.CalculateRepairCost(0.995f, 100);
            // (1 - 0.995) * 100 = 0.5, ceil(0.5) = 1
            Assert.AreEqual(1, cost);
        }

        [Test]
        public void RepairShipAction_DeductsCredits_RestoresIntegrity()
        {
            var eventBus = new UniTaskEventBus();
            var stationServices = StationServicesState.Empty with { Credits = 500 };

            var initialState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    stationServices,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty,
                    DockingState.Empty,
                    TargetingState.Empty
                ),
                ActiveShipPhysics: ShipState.Default with { HullIntegrity = 0.6f },
                Camera: CameraState.Default,
                World: new WorldState(ImmutableArray<StationData>.Empty, 0f)
            );

            var store = new StateStore(CompositeReducer, initialState, eventBus);

            store.Dispatch(new RepairShipAction(40, 1.0f));

            Assert.AreEqual(1.0f, store.Current.ActiveShipPhysics.HullIntegrity, 0.001f);
            Assert.AreEqual(460, store.Current.Loop.StationServices.Credits);

            store.Dispose();
            eventBus.Dispose();
        }

        [Test]
        public void RepairShipAction_InsufficientCredits_ReturnsUnchanged()
        {
            var eventBus = new UniTaskEventBus();
            var stationServices = StationServicesState.Empty with { Credits = 10 };

            var initialState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    stationServices,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty,
                    DockingState.Empty,
                    TargetingState.Empty
                ),
                ActiveShipPhysics: ShipState.Default with { HullIntegrity = 0.6f },
                Camera: CameraState.Default,
                World: new WorldState(ImmutableArray<StationData>.Empty, 0f)
            );

            var store = new StateStore(CompositeReducer, initialState, eventBus);
            var before = store.Current;

            store.Dispatch(new RepairShipAction(40, 1.0f));

            Assert.AreEqual(0.6f, store.Current.ActiveShipPhysics.HullIntegrity, 0.001f);
            Assert.AreEqual(10, store.Current.Loop.StationServices.Credits);

            store.Dispose();
            eventBus.Dispose();
        }

        [Test]
        public void RepairShipAction_At100Percent_ReturnsUnchanged()
        {
            var eventBus = new UniTaskEventBus();
            var stationServices = StationServicesState.Empty with { Credits = 500 };

            var initialState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    stationServices,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty,
                    DockingState.Empty,
                    TargetingState.Empty
                ),
                ActiveShipPhysics: ShipState.Default, // HullIntegrity defaults to 1.0f
                Camera: CameraState.Default,
                World: new WorldState(ImmutableArray<StationData>.Empty, 0f)
            );

            var store = new StateStore(CompositeReducer, initialState, eventBus);

            store.Dispatch(new RepairShipAction(0, 1.0f));

            Assert.AreEqual(500, store.Current.Loop.StationServices.Credits);

            store.Dispose();
            eventBus.Dispose();
        }

        /// <summary>Minimal composite reducer for repair testing.</summary>
        private static GameState CompositeReducer(GameState state, IGameAction action)
            => action switch
            {
                RepairShipAction a => HandleRepairShip(state, a),
                _ => state
            };

        private static GameState HandleRepairShip(GameState state, RepairShipAction a)
        {
            if (a.Cost <= 0 && state.ActiveShipPhysics.HullIntegrity >= 1.0f) return state;
            if (state.Loop.StationServices.Credits < a.Cost) return state;
            var updatedServices = state.Loop.StationServices with
            {
                Credits = state.Loop.StationServices.Credits - a.Cost
            };
            var updatedShip = state.ActiveShipPhysics with { HullIntegrity = a.NewIntegrity };
            return state with
            {
                Loop = state.Loop with { StationServices = updatedServices },
                ActiveShipPhysics = updatedShip
            };
        }
    }
}
