using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Data;
using VoidHarvest.Features.Station.Data;
using VoidHarvest.Features.World.Data;

namespace VoidHarvest.Features.World.Tests
{
    [TestFixture]
    public class WorldDefinitionValidationTests
    {
        private WorldDefinition _world;
        private StationDefinition _station1;
        private StationDefinition _station2;
        private ShipArchetypeConfig _shipArchetype;
        private StationServicesConfig _servicesConfig;

        [SetUp]
        public void SetUp()
        {
            _servicesConfig = ScriptableObject.CreateInstance<StationServicesConfig>();

            _station1 = ScriptableObject.CreateInstance<StationDefinition>();
            _station1.StationId = 1;
            _station1.DisplayName = "Station Alpha";
            _station1.AvailableServices = new[] { "Sell" };
            _station1.ServicesConfig = _servicesConfig;

            _station2 = ScriptableObject.CreateInstance<StationDefinition>();
            _station2.StationId = 2;
            _station2.DisplayName = "Station Beta";
            _station2.AvailableServices = new[] { "Sell", "Repair" };
            _station2.ServicesConfig = _servicesConfig;

            _shipArchetype = ScriptableObject.CreateInstance<ShipArchetypeConfig>();
            _shipArchetype.ArchetypeId = "test_ship";
            _shipArchetype.DisplayName = "Test Ship";

            _world = ScriptableObject.CreateInstance<WorldDefinition>();
            _world.Stations = new[] { _station1, _station2 };
            _world.StartingShipArchetype = _shipArchetype;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_world);
            Object.DestroyImmediate(_station1);
            Object.DestroyImmediate(_station2);
            Object.DestroyImmediate(_shipArchetype);
            Object.DestroyImmediate(_servicesConfig);
        }

        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            CallOnValidate(_world);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnValidate_NullStationEntry_LogsWarning()
        {
            _world.Stations = new StationDefinition[] { _station1, null };
            LogAssert.Expect(LogType.Warning, new Regex("Stations\\[1\\] must not be null"));
            CallOnValidate(_world);
        }

        [Test]
        public void OnValidate_DuplicateStationIds_LogsWarning()
        {
            _station2.StationId = 1; // same as _station1
            LogAssert.Expect(LogType.Warning, new Regex("Duplicate StationId 1"));
            CallOnValidate(_world);
        }

        [Test]
        public void OnValidate_EmptyStations_LogsWarning()
        {
            _world.Stations = new StationDefinition[0];
            LogAssert.Expect(LogType.Warning, new Regex("must have at least one station"));
            CallOnValidate(_world);
        }

        [Test]
        public void OnValidate_NullStationsArray_LogsWarning()
        {
            _world.Stations = null;
            LogAssert.Expect(LogType.Warning, new Regex("must have at least one station"));
            CallOnValidate(_world);
        }

        [Test]
        public void OnValidate_NullStartingShipArchetype_LogsWarning()
        {
            _world.StartingShipArchetype = null;
            LogAssert.Expect(LogType.Warning, new Regex("StartingShipArchetype must not be null"));
            CallOnValidate(_world);
        }

        private static void CallOnValidate(WorldDefinition def)
        {
            var method = typeof(WorldDefinition).GetMethod("OnValidate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "WorldDefinition must have an OnValidate method");
            method.Invoke(def, null);
        }
    }

    [TestFixture]
    public class WorldDefinitionMappingTests
    {
        private WorldDefinition _world;
        private StationDefinition _station1;
        private StationDefinition _station2;
        private ShipArchetypeConfig _shipArchetype;
        private StationServicesConfig _servicesConfig;

        [SetUp]
        public void SetUp()
        {
            _servicesConfig = ScriptableObject.CreateInstance<StationServicesConfig>();

            _station1 = ScriptableObject.CreateInstance<StationDefinition>();
            _station1.StationId = 1;
            _station1.DisplayName = "Small Mining Relay";
            _station1.WorldPosition = new Vector3(500f, 0f, 0f);
            _station1.AvailableServices = new[] { "Refinery", "Cargo" };
            _station1.ServicesConfig = _servicesConfig;

            _station2 = ScriptableObject.CreateInstance<StationDefinition>();
            _station2.StationId = 2;
            _station2.DisplayName = "Medium Refinery Hub";
            _station2.WorldPosition = new Vector3(-800f, 200f, 600f);
            _station2.AvailableServices = new[] { "Refinery", "Market", "Repair", "Cargo" };
            _station2.ServicesConfig = _servicesConfig;

            _shipArchetype = ScriptableObject.CreateInstance<ShipArchetypeConfig>();
            _shipArchetype.ArchetypeId = "test_ship";
            _shipArchetype.DisplayName = "Test Ship";

            _world = ScriptableObject.CreateInstance<WorldDefinition>();
            _world.Stations = new[] { _station1, _station2 };
            _world.StartingShipArchetype = _shipArchetype;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_world);
            Object.DestroyImmediate(_station1);
            Object.DestroyImmediate(_station2);
            Object.DestroyImmediate(_shipArchetype);
            Object.DestroyImmediate(_servicesConfig);
        }

        [Test]
        public void BuildWorldStations_ReturnsCorrectCount()
        {
            var stations = _world.BuildWorldStations();
            Assert.AreEqual(2, stations.Length);
        }

        [Test]
        public void BuildWorldStations_MapsStationIds()
        {
            var stations = _world.BuildWorldStations();
            Assert.AreEqual(1, stations[0].Id);
            Assert.AreEqual(2, stations[1].Id);
        }

        [Test]
        public void BuildWorldStations_MapsPositions()
        {
            var stations = _world.BuildWorldStations();
            Assert.AreEqual(new float3(500f, 0f, 0f), stations[0].Position);
            Assert.AreEqual(new float3(-800f, 200f, 600f), stations[1].Position);
        }

        [Test]
        public void BuildWorldStations_MapsNames()
        {
            var stations = _world.BuildWorldStations();
            Assert.AreEqual("Small Mining Relay", stations[0].Name);
            Assert.AreEqual("Medium Refinery Hub", stations[1].Name);
        }

        [Test]
        public void BuildWorldStations_MapsAvailableServices()
        {
            var stations = _world.BuildWorldStations();
            CollectionAssert.AreEqual(
                new[] { "Refinery", "Cargo" },
                stations[0].AvailableServices);
            CollectionAssert.AreEqual(
                new[] { "Refinery", "Market", "Repair", "Cargo" },
                stations[1].AvailableServices);
        }
    }

    [TestFixture]
    public class InventoryInitializationTests
    {
        [Test]
        public void InventoryState_DerivedFromShipArchetype_MaxSlots()
        {
            var archetype = ScriptableObject.CreateInstance<ShipArchetypeConfig>();
            archetype.CargoSlots = 15;
            archetype.CargoCapacity = 200f;

            var inventory = new InventoryState(
                ImmutableDictionary<string, ResourceStack>.Empty,
                archetype.CargoSlots,
                archetype.CargoCapacity,
                0f);

            Assert.AreEqual(15, inventory.MaxSlots);
            Assert.AreEqual(200f, inventory.MaxVolume, 0.001f);

            Object.DestroyImmediate(archetype);
        }

        [Test]
        public void InventoryState_DerivedFromShipArchetype_MaxVolume()
        {
            var archetype = ScriptableObject.CreateInstance<ShipArchetypeConfig>();
            archetype.CargoSlots = 30;
            archetype.CargoCapacity = 500f;

            var inventory = new InventoryState(
                ImmutableDictionary<string, ResourceStack>.Empty,
                archetype.CargoSlots,
                archetype.CargoCapacity,
                0f);

            Assert.AreEqual(30, inventory.MaxSlots);
            Assert.AreEqual(500f, inventory.MaxVolume, 0.001f);

            Object.DestroyImmediate(archetype);
        }
    }
}
