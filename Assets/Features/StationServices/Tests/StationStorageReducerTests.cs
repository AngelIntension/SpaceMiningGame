using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.StationServices.Systems;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class StationStorageReducerTests
    {
        [Test]
        public void AddResource_ToEmptyStorage_CreatesStack()
        {
            var state = StationStorageState.Empty;
            var result = StationStorageReducer.AddResource(state, "luminite", 10, 0.1f);

            Assert.IsTrue(result.Stacks.ContainsKey("luminite"));
            Assert.AreEqual(10, result.Stacks["luminite"].Quantity);
            Assert.AreEqual(0.1f, result.Stacks["luminite"].VolumePerUnit);
        }

        [Test]
        public void AddResource_ToExistingStack_IncrementsQuantity()
        {
            var state = StationStorageReducer.AddResource(StationStorageState.Empty, "luminite", 10, 0.1f);
            var result = StationStorageReducer.AddResource(state, "luminite", 5, 0.1f);

            Assert.AreEqual(15, result.Stacks["luminite"].Quantity);
        }

        [Test]
        public void RemoveResource_PartialAmount_DecrementsQuantity()
        {
            var state = StationStorageReducer.AddResource(StationStorageState.Empty, "luminite", 10, 0.1f);
            var result = StationStorageReducer.RemoveResource(state, "luminite", 3);

            Assert.AreEqual(7, result.Stacks["luminite"].Quantity);
        }

        [Test]
        public void RemoveResource_AllUnits_RemovesKey()
        {
            var state = StationStorageReducer.AddResource(StationStorageState.Empty, "luminite", 10, 0.1f);
            var result = StationStorageReducer.RemoveResource(state, "luminite", 10);

            Assert.IsFalse(result.Stacks.ContainsKey("luminite"));
        }

        [Test]
        public void RemoveResource_FromEmptyStorage_ReturnsUnchanged()
        {
            var state = StationStorageState.Empty;
            var result = StationStorageReducer.RemoveResource(state, "luminite", 5);

            Assert.AreSame(state, result);
        }

        [Test]
        public void RemoveResource_ExceedingQuantity_ReturnsUnchanged()
        {
            var state = StationStorageReducer.AddResource(StationStorageState.Empty, "luminite", 5, 0.1f);
            var result = StationStorageReducer.RemoveResource(state, "luminite", 10);

            Assert.AreSame(state, result);
        }

        [Test]
        public void AddResource_MultipleDistinctTypes_Coexist()
        {
            var state = StationStorageState.Empty;
            state = StationStorageReducer.AddResource(state, "luminite", 10, 0.1f);
            state = StationStorageReducer.AddResource(state, "ferrox", 5, 0.15f);
            state = StationStorageReducer.AddResource(state, "auralite", 2, 0.25f);

            Assert.AreEqual(3, state.Stacks.Count);
            Assert.AreEqual(10, state.Stacks["luminite"].Quantity);
            Assert.AreEqual(5, state.Stacks["ferrox"].Quantity);
            Assert.AreEqual(2, state.Stacks["auralite"].Quantity);
        }

        [Test]
        public void AddResource_ZeroQuantity_ReturnsUnchanged()
        {
            var state = StationStorageState.Empty;
            var result = StationStorageReducer.AddResource(state, "luminite", 0, 0.1f);

            Assert.AreSame(state, result);
        }
    }
}
