using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.Resources.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class CargoTransferTests
    {
        private StateStore _store;
        private UniTaskEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new UniTaskEventBus();

            // Build initial state with station storage initialized and ship with some cargo
            var stationServices = StationServicesState.Empty with
            {
                StationStorages = ImmutableDictionary<int, StationStorageState>.Empty
                    .Add(1, StationStorageState.Empty)
            };

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
        public void TransferToStation_RemovesFromShip_AddsToStation()
        {
            // Add cargo to ship
            _store.Dispatch(new AddResourceAction("luminite", 50, 0.1f));

            // Transfer 30 to station
            _store.Dispatch(new TransferToStationAction(1, "luminite", 30, 0.1f));

            Assert.AreEqual(20, _store.Current.Loop.Inventory.Stacks["luminite"].Quantity);
            Assert.AreEqual(30, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void TransferToShip_RemovesFromStation_AddsToShip()
        {
            // Add cargo to ship, transfer to station, then transfer back
            _store.Dispatch(new AddResourceAction("luminite", 50, 0.1f));
            _store.Dispatch(new TransferToStationAction(1, "luminite", 30, 0.1f));

            // Transfer 5 back to ship
            _store.Dispatch(new TransferToShipAction(1, "luminite", 5, 0.1f));

            Assert.AreEqual(25, _store.Current.Loop.Inventory.Stacks["luminite"].Quantity);
            Assert.AreEqual(25, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void TransferToShip_RejectedWhenShipVolumeFull()
        {
            // Fill ship cargo near capacity (MaxVolume = 100)
            _store.Dispatch(new AddResourceAction("ferrox", 90, 1.0f)); // 90 volume

            // Put luminite in station
            var services = _store.Current.Loop.StationServices;
            var addAction = new AddToStationStorageAction(1, "luminite", 20, 1.0f);
            _store.Dispatch(addAction);

            // Try to transfer 20 luminite (20 volume) to ship — 90 + 20 = 110 > 100
            var before = _store.Current;
            _store.Dispatch(new TransferToShipAction(1, "luminite", 20, 1.0f));

            // State should be unchanged (transfer rejected)
            Assert.AreEqual(90, _store.Current.Loop.Inventory.Stacks["ferrox"].Quantity);
            Assert.AreEqual(20, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void Transfer_ZeroQuantity_ReturnsUnchanged()
        {
            _store.Dispatch(new AddResourceAction("luminite", 10, 0.1f));
            var before = _store.Current;
            _store.Dispatch(new TransferToStationAction(1, "luminite", 0, 0.1f));
            Assert.AreSame(before, _store.Current);
        }

        [Test]
        public void Transfer_MoreThanSourceHas_ReturnsUnchanged()
        {
            _store.Dispatch(new AddResourceAction("luminite", 5, 0.1f));
            var before = _store.Current;
            _store.Dispatch(new TransferToStationAction(1, "luminite", 10, 0.1f));
            Assert.AreSame(before, _store.Current);
        }

        [Test]
        public void BothInventories_UpdateAtomically()
        {
            _store.Dispatch(new AddResourceAction("luminite", 50, 0.1f));
            _store.Dispatch(new TransferToStationAction(1, "luminite", 30, 0.1f));

            var ship = _store.Current.Loop.Inventory.Stacks["luminite"].Quantity;
            var station = _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite"].Quantity;

            Assert.AreEqual(50, ship + station, "Total quantity must be conserved");
        }

        [Test]
        public void SequentialTransfers_MaintainConsistency()
        {
            _store.Dispatch(new AddResourceAction("luminite", 100, 0.1f));

            for (int i = 0; i < 10; i++)
            {
                _store.Dispatch(new TransferToStationAction(1, "luminite", 5, 0.1f));
            }

            Assert.AreEqual(50, _store.Current.Loop.Inventory.Stacks["luminite"].Quantity);
            Assert.AreEqual(50, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void TransferRawMaterial_SucceedsInBothDirections()
        {
            // Add raw material to ship
            _store.Dispatch(new AddResourceAction("luminite_ingots", 20, 0.5f));

            // Transfer to station
            _store.Dispatch(new TransferToStationAction(1, "luminite_ingots", 10, 0.5f));
            Assert.AreEqual(10, _store.Current.Loop.Inventory.Stacks["luminite_ingots"].Quantity);
            Assert.AreEqual(10, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite_ingots"].Quantity);

            // Transfer back to ship
            _store.Dispatch(new TransferToShipAction(1, "luminite_ingots", 5, 0.5f));
            Assert.AreEqual(15, _store.Current.Loop.Inventory.Stacks["luminite_ingots"].Quantity);
            Assert.AreEqual(5, _store.Current.Loop.StationServices.StationStorages[1].Stacks["luminite_ingots"].Quantity);
        }

        /// <summary>
        /// Local composite reducer for testing. Mirrors RootLifetimeScope cross-cutting handlers.
        /// </summary>
        private static GameState CompositeReducer(GameState state, IGameAction action)
            => action switch
            {
                TransferToStationAction a => HandleTransferToStation(state, a),
                TransferToShipAction a => HandleTransferToShip(state, a),
                IInventoryAction a => state with { Loop = state.Loop with { Inventory = InventoryReducer.Reduce(state.Loop.Inventory, a) } },
                IStationServicesAction a => state with { Loop = state.Loop with { StationServices = StationServices.Systems.StationServicesReducer.Reduce(state.Loop.StationServices, a) } },
                _ => state
            };

        private static GameState HandleTransferToStation(GameState state, TransferToStationAction a)
        {
            if (a.Quantity <= 0) return state;
            var inventory = state.Loop.Inventory;
            if (!inventory.Stacks.TryGetValue(a.ResourceId, out var stack) || stack.Quantity < a.Quantity)
                return state;
            var updatedInventory = InventoryReducer.Reduce(inventory, new RemoveResourceAction(a.ResourceId, a.Quantity));
            if (ReferenceEquals(updatedInventory, inventory)) return state;
            var updatedServices = StationServices.Systems.StationServicesReducer.Reduce(
                state.Loop.StationServices, new AddToStationStorageAction(a.StationId, a.ResourceId, a.Quantity, a.VolumePerUnit));
            return state with { Loop = state.Loop with { Inventory = updatedInventory, StationServices = updatedServices } };
        }

        private static GameState HandleTransferToShip(GameState state, TransferToShipAction a)
        {
            if (a.Quantity <= 0) return state;
            var services = state.Loop.StationServices;
            if (!services.StationStorages.TryGetValue(a.StationId, out var storage)) return state;
            if (!storage.Stacks.TryGetValue(a.ResourceId, out var stack) || stack.Quantity < a.Quantity) return state;
            var inventory = state.Loop.Inventory;
            var updatedInventory = InventoryReducer.Reduce(inventory, new AddResourceAction(a.ResourceId, a.Quantity, a.VolumePerUnit));
            if (ReferenceEquals(updatedInventory, inventory)) return state;
            var updatedServices = StationServices.Systems.StationServicesReducer.Reduce(
                services, new RemoveFromStationStorageAction(a.StationId, a.ResourceId, a.Quantity));
            return state with { Loop = state.Loop with { Inventory = updatedInventory, StationServices = updatedServices } };
        }
    }
}
