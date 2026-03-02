using System.Collections.Immutable;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Mathematics;
using VoidHarvest.Core.EventBus;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Docking.Data;
using VoidHarvest.Features.Docking.Systems;
using VoidHarvest.Features.Ship.Systems;

namespace VoidHarvest.Features.Docking.Tests
{
    /// <summary>
    /// Integration tests for the docking pipeline: exercises the full
    /// reducer chain + math functions working together end-to-end.
    /// </summary>
    [TestFixture]
    public class DockingSystemTests
    {
        private StateStore _store;
        private UniTaskEventBus _eventBus;

        private static readonly float3 PortPos = new(200f, 0f, 0f);
        private static readonly quaternion PortRot = quaternion.identity;
        private const int StationId = 1;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new UniTaskEventBus();
            var initialState = new GameState(
                Loop: new GameLoopState(
                    ExploreState.Empty,
                    MiningSessionState.Empty,
                    InventoryState.Empty,
                    StationServicesState.Empty,
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

        // ------ Full Dock/Undock Pipeline ------

        [Test]
        public void FullDockPipeline_BeginDocking_CompleteDocking_DockAtStation()
        {
            // Begin docking: None → Approaching
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));

            var docking = _store.Current.Loop.Docking;
            Assert.AreEqual(DockingPhase.Approaching, docking.Phase);
            Assert.IsTrue(docking.IsInProgress);
            Assert.IsFalse(docking.IsDocked);

            // Complete docking: Approaching → Docked
            _store.Dispatch(new CompleteDockingAction(StationId));

            docking = _store.Current.Loop.Docking;
            Assert.AreEqual(DockingPhase.Docked, docking.Phase);
            Assert.IsTrue(docking.IsDocked);
            Assert.IsFalse(docking.IsInProgress);

            // Fleet state updated: DockedAtStation set
            _store.Dispatch(new DockAtStationAction(StationId));

            var fleet = _store.Current.Loop.Fleet;
            Assert.IsTrue(fleet.DockedAtStation.HasValue);
            Assert.AreEqual(StationId, fleet.DockedAtStation.GetValueOrDefault(-1));
        }

        [Test]
        public void FullUndockPipeline_Docked_BeginUndocking_CompleteUndocking()
        {
            // Set up docked state
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            _store.Dispatch(new CompleteDockingAction(StationId));
            _store.Dispatch(new DockAtStationAction(StationId));

            Assert.IsTrue(_store.Current.Loop.Docking.IsDocked);

            // Begin undocking: Docked → Undocking
            _store.Dispatch(new BeginUndockingAction());

            var docking = _store.Current.Loop.Docking;
            Assert.AreEqual(DockingPhase.Undocking, docking.Phase);
            Assert.IsFalse(docking.IsDocked);

            // Complete undocking: Undocking → None
            _store.Dispatch(new CompleteUndockingAction());

            docking = _store.Current.Loop.Docking;
            Assert.AreEqual(DockingPhase.None, docking.Phase);
            Assert.IsFalse(docking.IsDocked);
            Assert.IsFalse(docking.IsInProgress);
            Assert.IsFalse(docking.TargetStationId.HasValue);

            // Fleet state cleared
            _store.Dispatch(new UndockFromStationAction());

            var fleet = _store.Current.Loop.Fleet;
            Assert.IsFalse(fleet.DockedAtStation.HasValue);
        }

        [Test]
        public void FullCycleDockUndockDock_AllTransitionsCorrect()
        {
            // First dock
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            _store.Dispatch(new CompleteDockingAction(StationId));
            _store.Dispatch(new DockAtStationAction(StationId));
            Assert.IsTrue(_store.Current.Loop.Docking.IsDocked);

            // Undock
            _store.Dispatch(new BeginUndockingAction());
            _store.Dispatch(new CompleteUndockingAction());
            _store.Dispatch(new UndockFromStationAction());
            Assert.AreEqual(DockingPhase.None, _store.Current.Loop.Docking.Phase);

            // Dock again at different station
            var portPos2 = new float3(500f, 100f, 0f);
            _store.Dispatch(new BeginDockingAction(2, portPos2, PortRot));
            _store.Dispatch(new CompleteDockingAction(2));
            _store.Dispatch(new DockAtStationAction(2));

            Assert.IsTrue(_store.Current.Loop.Docking.IsDocked);
            Assert.AreEqual(2, _store.Current.Loop.Docking.TargetStationId.GetValueOrDefault(-1));
            Assert.AreEqual(2, _store.Current.Loop.Fleet.DockedAtStation.GetValueOrDefault(-1));
        }

        // ------ Cancel During Approach ------

        [Test]
        public void CancelDuringApproach_ResetsToNone()
        {
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            Assert.AreEqual(DockingPhase.Approaching, _store.Current.Loop.Docking.Phase);

            // Cancel (simulates manual thrust interruption)
            _store.Dispatch(new CancelDockingAction());

            var docking = _store.Current.Loop.Docking;
            Assert.AreEqual(DockingPhase.None, docking.Phase);
            Assert.IsFalse(docking.TargetStationId.HasValue);
            Assert.IsFalse(docking.DockingPortPosition.HasValue);
            Assert.IsFalse(docking.DockingPortRotation.HasValue);
        }

        [Test]
        public void CancelDuringApproach_FleetStateUnchanged()
        {
            // Fleet should NOT be updated during approach, only on completion
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            Assert.IsFalse(_store.Current.Loop.Fleet.DockedAtStation.HasValue);

            _store.Dispatch(new CancelDockingAction());
            Assert.IsFalse(_store.Current.Loop.Fleet.DockedAtStation.HasValue);
        }

        [Test]
        public void CancelThenRedock_WorksCorrectly()
        {
            // Start docking
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));

            // Cancel
            _store.Dispatch(new CancelDockingAction());
            Assert.AreEqual(DockingPhase.None, _store.Current.Loop.Docking.Phase);

            // Re-initiate docking at same station
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            Assert.AreEqual(DockingPhase.Approaching, _store.Current.Loop.Docking.Phase);

            // Complete this time
            _store.Dispatch(new CompleteDockingAction(StationId));
            Assert.IsTrue(_store.Current.Loop.Docking.IsDocked);
        }

        // ------ DockingMath Integration with State Machine ------

        [Test]
        public void SnapSequence_MathProducesValidPoseOverTime()
        {
            var startPos = new float3(0, 0, 0);
            var startRot = quaternion.identity;
            var targetPos = new float3(100, 50, 0);
            var targetRot = quaternion.AxisAngle(new float3(0, 1, 0), math.radians(90f));

            float snapDuration = 1.5f;
            float dt = 0.016f; // ~60fps
            int frames = (int)(snapDuration / dt) + 10; // extra frames to ensure completion

            float elapsed = 0f;
            float3 lastPos = startPos;
            bool reachedTarget = false;

            for (int i = 0; i < frames; i++)
            {
                elapsed += dt;
                float t = DockingMath.ComputeSnapProgress(elapsed, snapDuration);
                var (pos, rot) = DockingMath.InterpolateSnapPose(startPos, startRot, targetPos, targetRot, t);

                // Position should always be between start and target (monotonically approaching)
                float distToTarget = math.length(pos - targetPos);
                float distFromStart = math.length(pos - startPos);

                // Should converge toward target
                if (t >= 1f)
                {
                    Assert.AreEqual(targetPos.x, pos.x, 0.01f);
                    Assert.AreEqual(targetPos.y, pos.y, 0.01f);
                    reachedTarget = true;
                    break;
                }

                lastPos = pos;
            }

            Assert.IsTrue(reachedTarget, "Snap sequence should reach target within duration + buffer");
        }

        [Test]
        public void UndockClearance_ProducesPositionAwayFromPort()
        {
            var portPos = new float3(100, 0, 0);
            var portForward = math.forward(PortRot);
            float clearanceDist = 100f;

            var clearancePos = DockingMath.ComputeClearancePosition(portPos, portForward, clearanceDist);

            // Clearance position should be further from port origin
            float distFromPort = math.length(clearancePos - portPos);
            Assert.AreEqual(clearanceDist, distFromPort, 0.1f);
        }

        // ------ Flight Mode Integration ------

        [Test]
        public void DetermineFlightMode_DockedAlwaysReturnsDocked()
        {
            // Even with manual input, Docked never changes
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Docked, 1f, 1f, 1f, false, -1);
            Assert.AreEqual(ShipFlightMode.Docked, mode);
        }

        [Test]
        public void DetermineFlightMode_DockingWithNoInput_StaysDocking()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Docking, 0f, 0f, 0f, false, -1);
            Assert.AreEqual(ShipFlightMode.Docking, mode);
        }

        [Test]
        public void DetermineFlightMode_DockingWithManualInput_CancelsToManualThrust()
        {
            var mode = ShipPhysicsMath.DetermineFlightMode(
                ShipFlightMode.Docking, 1f, 0f, 0f, false, -1);
            Assert.AreEqual(ShipFlightMode.ManualThrust, mode);
        }

        // ------ Range Checks ------

        [Test]
        public void Initiation_BeyondDockingRange_RangeCheckFails()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(2500, 0, 0); // Beyond 500m max range

            Assert.IsFalse(DockingMath.IsWithinDockingRange(shipPos, portPos, 500f));
        }

        [Test]
        public void Initiation_WithinDockingRange_RangeCheckPasses()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(400, 0, 0); // Within 500m max range

            Assert.IsTrue(DockingMath.IsWithinDockingRange(shipPos, portPos, 500f));
        }

        [Test]
        public void SnapRange_ShipAtSnapDistance_TransitionsToSnapping()
        {
            var shipPos = new float3(0, 0, 0);
            var portPos = new float3(25, 0, 0); // Within 30m snap range

            Assert.IsTrue(DockingMath.IsWithinSnapRange(shipPos, portPos, 30f));
        }

        // ------ Performance ------

        [Test]
        public void DockingReducerLatency_Under2ms_PerDispatch()
        {
            // Warm up
            _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
            _store.Dispatch(new CancelDockingAction());

            var sw = Stopwatch.StartNew();
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                _store.Dispatch(new BeginDockingAction(StationId, PortPos, PortRot));
                _store.Dispatch(new CompleteDockingAction(StationId));
                _store.Dispatch(new DockAtStationAction(StationId));
                _store.Dispatch(new BeginUndockingAction());
                _store.Dispatch(new CompleteUndockingAction());
                _store.Dispatch(new UndockFromStationAction());
            }

            sw.Stop();
            double avgMs = sw.Elapsed.TotalMilliseconds / (iterations * 6);

            Assert.Less(avgMs, 2.0, $"Average docking reducer dispatch latency {avgMs:F4}ms exceeds 2ms target");
        }

        /// <summary>
        /// Local composite reducer routing docking and fleet actions.
        /// Mirrors RootLifetimeScope.CompositeReducer for test isolation.
        /// </summary>
        private static GameState CompositeReducer(GameState state, IGameAction action)
            => action switch
            {
                IDockingAction a => state with { Loop = state.Loop with { Docking = DockingReducer.Reduce(state.Loop.Docking, a) } },
                IFleetAction a   => state with { Loop = state.Loop with { Fleet = FleetReducer.Reduce(state.Loop.Fleet, a) } },
                _ => state
            };
    }
}
