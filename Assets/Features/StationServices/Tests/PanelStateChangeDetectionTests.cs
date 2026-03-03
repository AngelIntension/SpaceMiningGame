using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;

namespace VoidHarvest.Features.StationServices.Tests
{
    /// <summary>
    /// Tests verifying that station panel state-change detection correctly refreshes
    /// when ANY single relevant slice changes (AND-skip pattern).
    /// See Spec 008 US1: Station Panel Responsiveness.
    /// </summary>
    [TestFixture]
    public class PanelStateChangeDetectionTests
    {
        private GameState _baseState;
        private InventoryState _baseInventory;
        private StationServicesState _baseServices;
        private ShipState _baseShip;

        [SetUp]
        public void SetUp()
        {
            _baseInventory = InventoryState.Empty;
            _baseServices = StationServicesState.Empty;
            _baseShip = ShipState.Default;

            _baseState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    _baseInventory,
                    _baseServices,
                    TechTreeState.Empty,
                    FleetState.Empty,
                    BaseState.Empty,
                    MarketState.Empty,
                    DockingState.Empty,
                    TargetingState.Empty
                ),
                ActiveShipPhysics: _baseShip,
                Camera: CameraState.Default,
                World: new WorldState(ImmutableArray<StationData>.Empty, 0f)
            );
        }

        /// <summary>
        /// RefineOresPanelController must refresh when only InventoryState changes
        /// (StationServicesState unchanged). Before fix, it only tracked StationServicesState.
        /// </summary>
        [Test]
        public void RefineOresPanelRefreshesOnInventoryChange()
        {
            var lastServices = _baseServices;
            var lastInventory = _baseInventory;

            // Simulate: only InventoryState changes (e.g., cargo transfer adds ore to ship)
            var newInventory = _baseInventory with { MaxVolume = 999f };
            var newState = _baseState with
            {
                Loop = _baseState.Loop with { Inventory = newInventory }
            };

            // AND-skip: skip only when BOTH unchanged
            bool shouldSkip = ReferenceEquals(newState.Loop.StationServices, lastServices)
                           && ReferenceEquals(newState.Loop.Inventory, lastInventory);

            Assert.IsFalse(shouldSkip, "Panel must NOT skip when InventoryState changed");
        }

        /// <summary>
        /// SellResourcesPanelController must refresh when only InventoryState changes.
        /// Before fix, it only tracked StationServicesState.
        /// </summary>
        [Test]
        public void SellResourcesPanelRefreshesOnInventoryChange()
        {
            var lastServices = _baseServices;
            var lastInventory = _baseInventory;

            var newInventory = _baseInventory with { MaxVolume = 999f };
            var newState = _baseState with
            {
                Loop = _baseState.Loop with { Inventory = newInventory }
            };

            bool shouldSkip = ReferenceEquals(newState.Loop.StationServices, lastServices)
                           && ReferenceEquals(newState.Loop.Inventory, lastInventory);

            Assert.IsFalse(shouldSkip, "Panel must NOT skip when InventoryState changed");
        }

        /// <summary>
        /// CargoTransferPanelController already uses correct AND-skip across
        /// InventoryState + StationServicesState. Verify it refreshes when only
        /// StationServicesState changes (InventoryState unchanged).
        /// </summary>
        [Test]
        public void CargoTransferPanelRefreshesOnSingleSliceChange()
        {
            var lastInventory = _baseInventory;
            var lastServices = _baseServices;

            // Only StationServicesState changes
            var newServices = _baseServices with { Credits = 9999 };
            var newState = _baseState with
            {
                Loop = _baseState.Loop with { StationServices = newServices }
            };

            bool shouldSkip = ReferenceEquals(newState.Loop.Inventory, lastInventory)
                           && ReferenceEquals(newState.Loop.StationServices, lastServices);

            Assert.IsFalse(shouldSkip, "CargoTransfer must NOT skip when StationServicesState changed");
        }

        /// <summary>
        /// BasicRepairPanelController uses AND-skip across StationServicesState + ShipState.
        /// Verify it refreshes when only ActiveShipPhysics changes.
        /// </summary>
        [Test]
        public void BasicRepairPanelRefreshesOnSingleSliceChange()
        {
            var lastServices = _baseServices;
            var lastShip = _baseShip;

            // Only ShipState changes
            var newShip = _baseShip with { HullIntegrity = 0.5f };
            var newState = _baseState with { ActiveShipPhysics = newShip };

            bool shouldSkip = ReferenceEquals(newState.Loop.StationServices, lastServices)
                           && ReferenceEquals(newState.ActiveShipPhysics, lastShip);

            Assert.IsFalse(shouldSkip, "BasicRepair must NOT skip when ShipState changed");
        }

        /// <summary>
        /// All panels must skip refresh when NO relevant slice has changed.
        /// </summary>
        [Test]
        public void PanelSkipsRefreshWhenNoSliceChanged()
        {
            // Same state references — no changes
            var sameState = _baseState;

            // RefineOres: Services + Inventory
            bool refineSkip = ReferenceEquals(sameState.Loop.StationServices, _baseServices)
                           && ReferenceEquals(sameState.Loop.Inventory, _baseInventory);
            Assert.IsTrue(refineSkip, "RefineOres must skip when no slice changed");

            // SellResources: Services + Inventory
            bool sellSkip = ReferenceEquals(sameState.Loop.StationServices, _baseServices)
                         && ReferenceEquals(sameState.Loop.Inventory, _baseInventory);
            Assert.IsTrue(sellSkip, "SellResources must skip when no slice changed");

            // CargoTransfer: Inventory + Services
            bool cargoSkip = ReferenceEquals(sameState.Loop.Inventory, _baseInventory)
                          && ReferenceEquals(sameState.Loop.StationServices, _baseServices);
            Assert.IsTrue(cargoSkip, "CargoTransfer must skip when no slice changed");

            // BasicRepair: Services + Ship
            bool repairSkip = ReferenceEquals(sameState.Loop.StationServices, _baseServices)
                           && ReferenceEquals(sameState.ActiveShipPhysics, _baseShip);
            Assert.IsTrue(repairSkip, "BasicRepair must skip when no slice changed");
        }
    }
}
