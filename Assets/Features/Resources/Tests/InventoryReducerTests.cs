using System.Collections.Immutable;
using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Resources.Systems;

namespace VoidHarvest.Features.Resources.Tests
{
    [TestFixture]
    public class InventoryReducerTests
    {
        private InventoryState _emptyState;

        [SetUp]
        public void SetUp()
        {
            _emptyState = InventoryState.Empty;
        }

        // --- AddResourceAction: increases quantity in existing stack ---

        [Test]
        public void AddResource_ExistingStack_IncreasesQuantity()
        {
            var state = _emptyState with
            {
                Stacks = _emptyState.Stacks.Add("ore_veldspar",
                    new ResourceStack("ore_veldspar", 10, 0.5f)),
                CurrentVolume = 5f
            };

            var result = InventoryReducer.Reduce(state,
                new AddResourceAction("ore_veldspar", 5, 0.5f));

            Assert.AreEqual(15, result.Stacks["ore_veldspar"].Quantity);
        }

        // --- AddResourceAction: creates new stack when resource doesn't exist ---

        [Test]
        public void AddResource_NewResource_CreatesStack()
        {
            var result = InventoryReducer.Reduce(_emptyState,
                new AddResourceAction("ore_veldspar", 10, 0.5f));

            Assert.IsTrue(result.Stacks.ContainsKey("ore_veldspar"));
            Assert.AreEqual(10, result.Stacks["ore_veldspar"].Quantity);
            Assert.AreEqual(0.5f, result.Stacks["ore_veldspar"].VolumePerUnit, 0.001f);
        }

        // --- AddResourceAction: updates CurrentVolume correctly ---

        [Test]
        public void AddResource_UpdatesCurrentVolume()
        {
            var result = InventoryReducer.Reduce(_emptyState,
                new AddResourceAction("ore_veldspar", 10, 2.0f));

            // 10 units * 2.0 volume each = 20.0 added to 0.0 base
            Assert.AreEqual(20f, result.CurrentVolume, 0.001f);
        }

        [Test]
        public void AddResource_ExistingStack_UpdatesCurrentVolume()
        {
            var state = _emptyState with
            {
                Stacks = _emptyState.Stacks.Add("ore_veldspar",
                    new ResourceStack("ore_veldspar", 10, 2.0f)),
                CurrentVolume = 20f
            };

            var result = InventoryReducer.Reduce(state,
                new AddResourceAction("ore_veldspar", 5, 2.0f));

            // 20.0 existing + (5 * 2.0) = 30.0
            Assert.AreEqual(30f, result.CurrentVolume, 0.001f);
        }

        // --- AddResourceAction: rejects when volume would exceed MaxVolume ---

        [Test]
        public void AddResource_ExceedsMaxVolume_ReturnsUnchangedState()
        {
            var state = _emptyState with { CurrentVolume = 95f };

            var result = InventoryReducer.Reduce(state,
                new AddResourceAction("ore_veldspar", 10, 2.0f));

            // 95 + (10 * 2.0) = 115 > MaxVolume 100 => rejected
            Assert.AreSame(state, result);
        }

        // --- AddResourceAction: rejects when MaxSlots exceeded for new resource ---

        [Test]
        public void AddResource_ExceedsMaxSlots_ReturnsUnchangedState()
        {
            // Create a state with MaxSlots = 2, already holding 2 different resources
            var stacks = ImmutableDictionary<string, ResourceStack>.Empty
                .Add("ore_a", new ResourceStack("ore_a", 5, 1.0f))
                .Add("ore_b", new ResourceStack("ore_b", 5, 1.0f));
            var state = _emptyState with
            {
                Stacks = stacks,
                MaxSlots = 2,
                CurrentVolume = 10f
            };

            var result = InventoryReducer.Reduce(state,
                new AddResourceAction("ore_c", 1, 1.0f));

            // Adding a third resource when MaxSlots is 2 => rejected
            Assert.AreSame(state, result);
        }

        [Test]
        public void AddResource_ExistingResource_AtMaxSlots_Succeeds()
        {
            // Adding to an existing stack should succeed even at max slots
            var stacks = ImmutableDictionary<string, ResourceStack>.Empty
                .Add("ore_a", new ResourceStack("ore_a", 5, 1.0f))
                .Add("ore_b", new ResourceStack("ore_b", 5, 1.0f));
            var state = _emptyState with
            {
                Stacks = stacks,
                MaxSlots = 2,
                CurrentVolume = 10f
            };

            var result = InventoryReducer.Reduce(state,
                new AddResourceAction("ore_a", 3, 1.0f));

            Assert.AreEqual(8, result.Stacks["ore_a"].Quantity);
        }

        // --- RemoveResourceAction: decreases quantity ---

        [Test]
        public void RemoveResource_DecreasesQuantity()
        {
            var stacks = _emptyState.Stacks.Add("ore_veldspar",
                new ResourceStack("ore_veldspar", 10, 0.5f));
            var state = _emptyState with { Stacks = stacks, CurrentVolume = 5f };

            var result = InventoryReducer.Reduce(state,
                new RemoveResourceAction("ore_veldspar", 3));

            Assert.AreEqual(7, result.Stacks["ore_veldspar"].Quantity);
        }

        // --- RemoveResourceAction: rejects when quantity is insufficient ---

        [Test]
        public void RemoveResource_InsufficientQuantity_ReturnsUnchangedState()
        {
            var stacks = _emptyState.Stacks.Add("ore_veldspar",
                new ResourceStack("ore_veldspar", 5, 0.5f));
            var state = _emptyState with { Stacks = stacks, CurrentVolume = 2.5f };

            var result = InventoryReducer.Reduce(state,
                new RemoveResourceAction("ore_veldspar", 10));

            Assert.AreSame(state, result);
        }

        [Test]
        public void RemoveResource_NonExistentResource_ReturnsUnchangedState()
        {
            var result = InventoryReducer.Reduce(_emptyState,
                new RemoveResourceAction("ore_doesnt_exist", 1));

            Assert.AreSame(_emptyState, result);
        }

        // --- RemoveResourceAction: clears entry when last unit removed ---

        [Test]
        public void RemoveResource_LastUnit_RemovesStackFromDictionary()
        {
            var stacks = _emptyState.Stacks.Add("ore_veldspar",
                new ResourceStack("ore_veldspar", 5, 2.0f));
            var state = _emptyState with { Stacks = stacks, CurrentVolume = 10f };

            var result = InventoryReducer.Reduce(state,
                new RemoveResourceAction("ore_veldspar", 5));

            Assert.IsFalse(result.Stacks.ContainsKey("ore_veldspar"));
            Assert.AreEqual(0, result.Stacks.Count);
        }

        // --- RemoveResourceAction: updates CurrentVolume correctly ---

        [Test]
        public void RemoveResource_UpdatesCurrentVolume()
        {
            var stacks = _emptyState.Stacks.Add("ore_veldspar",
                new ResourceStack("ore_veldspar", 10, 2.0f));
            var state = _emptyState with { Stacks = stacks, CurrentVolume = 20f };

            var result = InventoryReducer.Reduce(state,
                new RemoveResourceAction("ore_veldspar", 3));

            // 20.0 - (3 * 2.0) = 14.0
            Assert.AreEqual(14f, result.CurrentVolume, 0.001f);
        }

        [Test]
        public void RemoveResource_AllUnits_CurrentVolumeReturnsToZero()
        {
            var stacks = _emptyState.Stacks.Add("ore_veldspar",
                new ResourceStack("ore_veldspar", 10, 2.0f));
            var state = _emptyState with { Stacks = stacks, CurrentVolume = 20f };

            var result = InventoryReducer.Reduce(state,
                new RemoveResourceAction("ore_veldspar", 10));

            Assert.AreEqual(0f, result.CurrentVolume, 0.001f);
        }

        // --- Unknown action returns unchanged state ---

        [Test]
        public void UnknownAction_ReturnsUnchangedState()
        {
            var result = InventoryReducer.Reduce(_emptyState, new UnknownInventoryAction());
            Assert.AreSame(_emptyState, result);
        }

        // --- AddResource with zero quantity returns unchanged state ---

        [Test]
        public void AddResource_ZeroQuantity_ReturnsUnchangedState()
        {
            var result = InventoryReducer.Reduce(_emptyState,
                new AddResourceAction("ore_veldspar", 0, 0.5f));

            Assert.AreSame(_emptyState, result);
        }

        /// <summary>
        /// Dummy action to verify the reducer returns unchanged state for unrecognized actions.
        /// </summary>
        private sealed record UnknownInventoryAction() : IInventoryAction;
    }
}
