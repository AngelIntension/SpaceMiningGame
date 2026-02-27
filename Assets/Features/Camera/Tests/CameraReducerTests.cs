using NUnit.Framework;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Camera.Systems;

namespace VoidHarvest.Features.Camera.Tests
{
    [TestFixture]
    public class CameraReducerTests
    {
        private CameraState _defaultState;

        [SetUp]
        public void SetUp()
        {
            _defaultState = CameraState.Default;
        }

        // OrbitAction tests
        [Test]
        public void OrbitAction_AddsYawDelta()
        {
            var result = CameraReducer.Reduce(_defaultState, new OrbitAction(10f, 0f));
            Assert.AreEqual(_defaultState.OrbitYaw + 10f, result.OrbitYaw, 0.001f);
        }

        [Test]
        public void OrbitAction_AddsPitchDelta()
        {
            var result = CameraReducer.Reduce(_defaultState, new OrbitAction(0f, 5f));
            Assert.AreEqual(_defaultState.OrbitPitch + 5f, result.OrbitPitch, 0.001f);
        }

        [Test]
        public void OrbitAction_ClampsPitch_AtMax80()
        {
            var state = _defaultState with { OrbitPitch = 75f };
            var result = CameraReducer.Reduce(state, new OrbitAction(0f, 10f));
            Assert.AreEqual(80f, result.OrbitPitch, 0.001f);
        }

        [Test]
        public void OrbitAction_ClampsPitch_AtMinNeg80()
        {
            var state = _defaultState with { OrbitPitch = -75f };
            var result = CameraReducer.Reduce(state, new OrbitAction(0f, -10f));
            Assert.AreEqual(-80f, result.OrbitPitch, 0.001f);
        }

        // ZoomAction tests
        [Test]
        public void ZoomAction_AdjustsTargetDistance()
        {
            var result = CameraReducer.Reduce(_defaultState, new ZoomAction(-5f));
            Assert.AreEqual(_defaultState.TargetDistance - 5f, result.TargetDistance, 0.001f);
        }

        [Test]
        public void ZoomAction_ClampsMin5()
        {
            var state = _defaultState with { TargetDistance = 7f };
            var result = CameraReducer.Reduce(state, new ZoomAction(-10f));
            Assert.AreEqual(5f, result.TargetDistance, 0.001f);
        }

        [Test]
        public void ZoomAction_ClampsMax50()
        {
            var state = _defaultState with { TargetDistance = 48f };
            var result = CameraReducer.Reduce(state, new ZoomAction(10f));
            Assert.AreEqual(50f, result.TargetDistance, 0.001f);
        }

        // SpeedZoomAction tests
        [Test]
        public void SpeedZoomAction_ZeroSpeed_SetsMaxZoomDistance()
        {
            var result = CameraReducer.Reduce(_defaultState, new SpeedZoomAction(0f));
            // At speed 0, should be at far distance
            Assert.Greater(result.TargetDistance, 30f);
        }

        [Test]
        public void SpeedZoomAction_FullSpeed_SetsMinZoomDistance()
        {
            var result = CameraReducer.Reduce(_defaultState, new SpeedZoomAction(1f));
            // At full speed, should be at close distance
            Assert.Less(result.TargetDistance, 15f);
        }

        [Test]
        public void SpeedZoomAction_ClampsNormalizedSpeed()
        {
            // Over 1.0 should be clamped
            var result1 = CameraReducer.Reduce(_defaultState, new SpeedZoomAction(1.5f));
            var result2 = CameraReducer.Reduce(_defaultState, new SpeedZoomAction(1.0f));
            Assert.AreEqual(result2.TargetDistance, result1.TargetDistance, 0.001f);
        }

        // ToggleFreeLookAction tests
        [Test]
        public void ToggleFreeLook_TogglesActive()
        {
            var result = CameraReducer.Reduce(_defaultState, new ToggleFreeLookAction());
            Assert.IsTrue(result.FreeLookActive);
        }

        [Test]
        public void ToggleFreeLook_ResetsOffsets()
        {
            var state = _defaultState with { FreeLookActive = true, FreeLookYaw = 30f, FreeLookPitch = -10f };
            var result = CameraReducer.Reduce(state, new ToggleFreeLookAction());
            Assert.IsFalse(result.FreeLookActive);
            Assert.AreEqual(0f, result.FreeLookYaw, 0.001f);
            Assert.AreEqual(0f, result.FreeLookPitch, 0.001f);
        }

        // FreeLookAction tests
        [Test]
        public void FreeLookAction_WhenActive_AppliesOffset()
        {
            var state = _defaultState with { FreeLookActive = true };
            var result = CameraReducer.Reduce(state, new FreeLookAction(15f, -5f));
            Assert.AreEqual(15f, result.FreeLookYaw, 0.001f);
            Assert.AreEqual(-5f, result.FreeLookPitch, 0.001f);
        }

        [Test]
        public void FreeLookAction_WhenInactive_NoEffect()
        {
            var state = _defaultState with { FreeLookActive = false };
            var result = CameraReducer.Reduce(state, new FreeLookAction(15f, -5f));
            Assert.AreEqual(0f, result.FreeLookYaw, 0.001f);
            Assert.AreEqual(0f, result.FreeLookPitch, 0.001f);
        }

        [Test]
        public void FreeLookAction_ClampsPitch()
        {
            var state = _defaultState with { FreeLookActive = true, FreeLookPitch = 75f };
            var result = CameraReducer.Reduce(state, new FreeLookAction(0f, 10f));
            Assert.AreEqual(80f, result.FreeLookPitch, 0.001f);
        }

        // Unknown action
        [Test]
        public void UnknownAction_ReturnsUnchangedState()
        {
            var result = CameraReducer.Reduce(_defaultState, new UnknownCameraAction());
            Assert.AreSame(_defaultState, result);
        }

        private sealed record UnknownCameraAction() : ICameraAction;
    }
}
