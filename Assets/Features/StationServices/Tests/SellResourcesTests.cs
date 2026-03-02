using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class SellResourcesTests
    {
        private StationServicesState _state;

        [SetUp]
        public void SetUp()
        {
            // Start with 1 station, some luminite in storage, 0 credits
            var storage = StationStorageState.Empty;
            storage = StationStorageReducer.AddResource(storage, "luminite", 20, 0.1f);

            _state = StationServicesState.Empty with
            {
                StationStorages = ImmutableDictionary<int, StationStorageState>.Empty.Add(1, storage)
            };
        }

        [Test]
        public void SellResource_RemovesFromStorage_AddsCredits()
        {
            var result = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 10, 10));

            Assert.AreEqual(10, result.StationStorages[1].Stacks["luminite"].Quantity);
            Assert.AreEqual(100, result.Credits); // 10 * 10
        }

        [Test]
        public void SellResource_AllUnits_RemovesKeyFromStorage()
        {
            var result = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 20, 10));

            Assert.IsFalse(result.StationStorages[1].Stacks.ContainsKey("luminite"));
            Assert.AreEqual(200, result.Credits); // 20 * 10
        }

        [Test]
        public void SellResource_EmptyStorage_ReturnsUnchanged()
        {
            var emptyState = StationServicesState.Empty with
            {
                StationStorages = ImmutableDictionary<int, StationStorageState>.Empty
                    .Add(1, StationStorageState.Empty)
            };
            var result = StationServicesReducer.Reduce(emptyState,
                new SellResourceAction(1, "luminite", 5, 10));

            Assert.AreSame(emptyState, result);
        }

        [Test]
        public void SellResource_ZeroQuantity_ReturnsUnchanged()
        {
            var result = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 0, 10));

            Assert.AreSame(_state, result);
        }

        [Test]
        public void SellResource_CreditsAreInt()
        {
            var result = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 3, 25));

            // 3 * 25 = 75 (integer arithmetic)
            Assert.AreEqual(75, result.Credits);
            Assert.IsInstanceOf<int>(result.Credits);
        }

        [Test]
        public void SellResource_MultipleSales_AccumulateCredits()
        {
            var state = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 5, 10)); // +50
            state = StationServicesReducer.Reduce(state,
                new SellResourceAction(1, "luminite", 5, 10)); // +50

            Assert.AreEqual(100, state.Credits);
            Assert.AreEqual(10, state.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void SellResource_ExceedingAvailable_ReturnsUnchanged()
        {
            var result = StationServicesReducer.Reduce(_state,
                new SellResourceAction(1, "luminite", 30, 10)); // only have 20

            Assert.AreSame(_state, result);
        }
    }
}
