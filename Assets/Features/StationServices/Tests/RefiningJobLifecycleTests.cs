using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Data;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class RefiningJobLifecycleTests
    {
        private StationServicesState _state;

        [SetUp]
        public void SetUp()
        {
            var storage = StationStorageState.Empty;
            storage = StationStorageReducer.AddResource(storage, "luminite", 50, 0.1f);

            _state = StationServicesState.Empty with
            {
                Credits = 1000,
                StationStorages = ImmutableDictionary<int, StationStorageState>.Empty.Add(1, storage)
            };
        }

        [Test]
        public void StartRefiningJob_RemovesOre_DeductsCredits_AddsJob()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));
            var action = new StartRefiningJobAction(1, "luminite", 10, 50, 30f, configs, 3, 0f);

            var result = StationServicesReducer.Reduce(_state, action);

            Assert.AreEqual(40, result.StationStorages[1].Stacks["luminite"].Quantity); // 50 - 10
            Assert.AreEqual(950, result.Credits); // 1000 - 50
            Assert.AreEqual(1, result.RefiningJobs[1].Length);
            Assert.AreEqual(RefiningJobStatus.Active, result.RefiningJobs[1][0].Status);
        }

        [Test]
        public void CompleteRefiningJob_SetsCompleted_StoresOutputs()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));
            var state = StationServicesReducer.Reduce(_state,
                new StartRefiningJobAction(1, "luminite", 10, 50, 30f, configs, 3, 0f));

            var jobId = state.RefiningJobs[1][0].JobId;
            var outputs = ImmutableArray.Create(new MaterialOutput("luminite_ingots", 35));

            var result = StationServicesReducer.Reduce(state,
                new CompleteRefiningJobAction(1, jobId, outputs));

            Assert.AreEqual(RefiningJobStatus.Completed, result.RefiningJobs[1][0].Status);
            Assert.AreEqual(35, result.RefiningJobs[1][0].GeneratedOutputs[0].Quantity);
            // NOT added to storage yet
            Assert.IsFalse(result.StationStorages[1].Stacks.ContainsKey("luminite_ingots"));
        }

        [Test]
        public void CollectRefiningJob_AddsToStorage_RemovesJob()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));
            var state = StationServicesReducer.Reduce(_state,
                new StartRefiningJobAction(1, "luminite", 10, 50, 30f, configs, 3, 0f));

            var jobId = state.RefiningJobs[1][0].JobId;
            var outputs = ImmutableArray.Create(new MaterialOutput("luminite_ingots", 35));

            state = StationServicesReducer.Reduce(state,
                new CompleteRefiningJobAction(1, jobId, outputs));

            var result = StationServicesReducer.Reduce(state,
                new CollectRefiningJobAction(1, jobId));

            Assert.AreEqual(35, result.StationStorages[1].Stacks["luminite_ingots"].Quantity);
            Assert.AreEqual(0, result.RefiningJobs[1].Length); // Job removed
        }

        [Test]
        public void StartRefiningJob_InsufficientCredits_ReturnsUnchanged()
        {
            var poorState = _state with { Credits = 10 };
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));

            var result = StationServicesReducer.Reduce(poorState,
                new StartRefiningJobAction(1, "luminite", 10, 50, 30f, configs, 3, 0f));

            Assert.AreSame(poorState, result);
        }

        [Test]
        public void StartRefiningJob_InsufficientOre_ReturnsUnchanged()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));

            var result = StationServicesReducer.Reduce(_state,
                new StartRefiningJobAction(1, "luminite", 100, 500, 30f, configs, 3, 0f)); // only have 50

            Assert.AreSame(_state, result);
        }

        [Test]
        public void StartRefiningJob_MaxSlotsEnforced()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));

            // Fill all 2 slots
            var state = StationServicesReducer.Reduce(_state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));
            state = StationServicesReducer.Reduce(state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));

            Assert.AreEqual(2, state.RefiningJobs[1].Length);

            // Third should fail
            var result = StationServicesReducer.Reduce(state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));

            Assert.AreSame(state, result);
        }

        [Test]
        public void CompletedJob_FreesSlotImmediately()
        {
            var configs = ImmutableArray.Create(new RefiningOutputConfig("luminite_ingots", 4, -1, 2));

            // Fill 2 slots
            var state = StationServicesReducer.Reduce(_state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));
            state = StationServicesReducer.Reduce(state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));

            // Complete first job
            var jobId = state.RefiningJobs[1][0].JobId;
            var outputs = ImmutableArray.Create(new MaterialOutput("luminite_ingots", 20));
            state = StationServicesReducer.Reduce(state,
                new CompleteRefiningJobAction(1, jobId, outputs));

            // Now only 1 active slot used, should be able to start a new job
            var result = StationServicesReducer.Reduce(state,
                new StartRefiningJobAction(1, "luminite", 5, 25, 30f, configs, 2, 0f));

            // 2 existing + 1 new = 3 total (but only 2 active)
            Assert.AreEqual(3, result.RefiningJobs[1].Length);
        }
    }
}
