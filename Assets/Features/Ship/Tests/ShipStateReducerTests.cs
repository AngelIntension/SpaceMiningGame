using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Ship.Tests
{
    [TestFixture]
    public class ShipStateReducerTests
    {
        private ShipState _defaultState;

        [SetUp]
        public void SetUp()
        {
            _defaultState = ShipState.Default;
        }

        [Test]
        public void SyncShipPhysicsAction_CopiesAllFields()
        {
            var action = new SyncShipPhysicsAction(
                new float3(10, 20, 30),
                quaternion.EulerXYZ(0.1f, 0.2f, 0.3f),
                new float3(1, 2, 3),
                new float3(0.1f, 0.2f, 0.3f),
                ShipFlightMode.ManualThrust
            );

            var result = ShipStateReducer.Reduce(_defaultState, action);

            Assert.AreEqual(new float3(10, 20, 30), result.Position);
            Assert.AreEqual(new float3(1, 2, 3), result.Velocity);
            Assert.AreEqual(new float3(0.1f, 0.2f, 0.3f), result.AngularVelocity);
            Assert.AreEqual(ShipFlightMode.ManualThrust, result.FlightMode);
        }

        [Test]
        public void SyncShipPhysicsAction_PreservesNonSyncedFields()
        {
            var action = new SyncShipPhysicsAction(
                float3.zero, quaternion.identity, float3.zero, float3.zero,
                ShipFlightMode.Idle
            );

            var result = ShipStateReducer.Reduce(_defaultState, action);

            // These fields are NOT part of SyncShipPhysicsAction
            Assert.AreEqual(_defaultState.Mass, result.Mass);
            Assert.AreEqual(_defaultState.MaxThrust, result.MaxThrust);
            Assert.AreEqual(_defaultState.MaxSpeed, result.MaxSpeed);
            Assert.AreEqual(_defaultState.HullIntegrity, result.HullIntegrity);
        }

        [Test]
        public void UnknownAction_ReturnsUnchangedState()
        {
            var result = ShipStateReducer.Reduce(_defaultState, new UnknownShipAction());
            Assert.AreSame(_defaultState, result);
        }

        private sealed record UnknownShipAction() : IShipAction;
    }
}
