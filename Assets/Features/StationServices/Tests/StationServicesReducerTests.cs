using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class StationServicesReducerTests
    {
        [Test]
        public void SetCreditsAction_ReplacesBalance()
        {
            var state = StationServicesState.Empty;
            var result = StationServicesReducer.Reduce(state, new SetCreditsAction(500));

            Assert.AreEqual(500, result.Credits);
        }

        [Test]
        public void InitializeStationStorageAction_CreatesEmptyEntry()
        {
            var state = StationServicesState.Empty;
            var result = StationServicesReducer.Reduce(state, new InitializeStationStorageAction(1));

            Assert.IsTrue(result.StationStorages.ContainsKey(1));
            Assert.AreEqual(0, result.StationStorages[1].Stacks.Count);
        }

        [Test]
        public void AddToStationStorageAction_AddsResource()
        {
            var state = StationServicesState.Empty;
            state = StationServicesReducer.Reduce(state, new InitializeStationStorageAction(1));
            var result = StationServicesReducer.Reduce(state,
                new AddToStationStorageAction(1, "luminite", 10, 0.1f));

            Assert.IsTrue(result.StationStorages[1].Stacks.ContainsKey("luminite"));
            Assert.AreEqual(10, result.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void RemoveFromStationStorageAction_RemovesResource()
        {
            var state = StationServicesState.Empty;
            state = StationServicesReducer.Reduce(state, new InitializeStationStorageAction(1));
            state = StationServicesReducer.Reduce(state,
                new AddToStationStorageAction(1, "luminite", 10, 0.1f));
            var result = StationServicesReducer.Reduce(state,
                new RemoveFromStationStorageAction(1, "luminite", 3));

            Assert.AreEqual(7, result.StationStorages[1].Stacks["luminite"].Quantity);
        }

        [Test]
        public void OperationOnNonExistentStation_ReturnsUnchanged()
        {
            var state = StationServicesState.Empty;
            var result = StationServicesReducer.Reduce(state,
                new AddToStationStorageAction(99, "luminite", 10, 0.1f));

            Assert.AreSame(state, result);
        }

        [Test]
        public void SetCreditsAction_CanSetToZero()
        {
            var state = StationServicesState.Empty with { Credits = 500 };
            var result = StationServicesReducer.Reduce(state, new SetCreditsAction(0));

            Assert.AreEqual(0, result.Credits);
        }

        [Test]
        public void UnknownAction_ReturnsUnchanged()
        {
            var state = StationServicesState.Empty;
            var result = StationServicesReducer.Reduce(state, new UnknownTestAction());

            Assert.AreSame(state, result);
        }

        private sealed record UnknownTestAction() : IStationServicesAction;
    }
}
