using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Core.State;
using VoidHarvest.Features.Camera.Data;
using VoidHarvest.Features.Camera.Systems;

namespace VoidHarvest.Features.Camera.Tests
{
    [TestFixture]
    public class CameraConfigValidationTests
    {
        private CameraConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CameraConfig>();
            _config.MinPitch = -80f;
            _config.MaxPitch = 80f;
            _config.MinDistance = 5f;
            _config.MaxDistance = 50f;
            _config.MinZoomDistance = 10f;
            _config.MaxZoomDistance = 40f;
            _config.ZoomCooldownDuration = 2.0f;
            _config.DefaultYaw = 0f;
            _config.DefaultPitch = 15f;
            _config.DefaultDistance = 25f;
            _config.OrbitSensitivity = 0.1f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            CallOnValidate(_config);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnValidate_MinPitchNotNegative_LogsWarning()
        {
            _config.MinPitch = 5f;
            LogAssert.Expect(LogType.Warning, new Regex("MinPitch"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MaxPitchNotPositive_LogsWarning()
        {
            _config.MaxPitch = -5f;
            LogAssert.Expect(LogType.Warning, new Regex("MaxPitch"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MinDistanceZero_LogsWarning()
        {
            _config.MinDistance = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("MinDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MaxDistanceNotGreaterThanMin_LogsWarning()
        {
            _config.MaxDistance = 5f;
            _config.MinDistance = 5f;
            LogAssert.Expect(LogType.Warning, new Regex("MaxDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MinZoomDistanceBelowMinDistance_LogsWarning()
        {
            _config.MinZoomDistance = 3f;
            LogAssert.Expect(LogType.Warning, new Regex("MinZoomDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MaxZoomDistanceAboveMaxDistance_LogsWarning()
        {
            _config.MaxZoomDistance = 55f;
            LogAssert.Expect(LogType.Warning, new Regex("MaxZoomDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_ZoomCooldownNegative_LogsWarning()
        {
            _config.ZoomCooldownDuration = -1f;
            LogAssert.Expect(LogType.Warning, new Regex("ZoomCooldownDuration"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_OrbitSensitivityZero_LogsWarning()
        {
            _config.OrbitSensitivity = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("OrbitSensitivity"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DefaultPitchOutOfRange_LogsWarning()
        {
            _config.DefaultPitch = 90f;
            LogAssert.Expect(LogType.Warning, new Regex("DefaultPitch"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DefaultDistanceOutOfRange_LogsWarning()
        {
            _config.DefaultDistance = 60f;
            LogAssert.Expect(LogType.Warning, new Regex("DefaultDistance"));
            CallOnValidate(_config);
        }

        private static void CallOnValidate(CameraConfig config)
        {
            var method = typeof(CameraConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "CameraConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }

    [TestFixture]
    public class CameraStateLimitsTests
    {
        [Test]
        public void CameraState_WithLimits_HasCorrectDefaults()
        {
            var state = CameraState.Default;
            Assert.AreEqual(-80f, state.MinPitch, 0.001f);
            Assert.AreEqual(80f, state.MaxPitch, 0.001f);
            Assert.AreEqual(5f, state.MinDistance, 0.001f);
            Assert.AreEqual(50f, state.MaxDistance, 0.001f);
            Assert.AreEqual(10f, state.MinZoomDistance, 0.001f);
            Assert.AreEqual(40f, state.MaxZoomDistance, 0.001f);
        }

        [Test]
        public void CameraReducer_OrbitAction_RespectsStateLimits()
        {
            var state = CameraState.Default with { MinPitch = -45f, MaxPitch = 45f, OrbitPitch = 40f };
            var result = CameraReducer.Reduce(state, new OrbitAction(0f, 10f));
            Assert.AreEqual(45f, result.OrbitPitch, 0.001f);
        }

        [Test]
        public void CameraReducer_OrbitAction_RespectsNegativeStateLimits()
        {
            var state = CameraState.Default with { MinPitch = -45f, MaxPitch = 45f, OrbitPitch = -40f };
            var result = CameraReducer.Reduce(state, new OrbitAction(0f, -10f));
            Assert.AreEqual(-45f, result.OrbitPitch, 0.001f);
        }

        [Test]
        public void CameraReducer_ZoomAction_RespectsStateDistanceLimits()
        {
            var state = CameraState.Default with { MinDistance = 10f, MaxDistance = 30f, TargetDistance = 12f };
            var result = CameraReducer.Reduce(state, new ZoomAction(-10f));
            Assert.AreEqual(10f, result.TargetDistance, 0.001f);
        }

        [Test]
        public void CameraReducer_ZoomAction_RespectsMaxDistanceLimit()
        {
            var state = CameraState.Default with { MinDistance = 10f, MaxDistance = 30f, TargetDistance = 28f };
            var result = CameraReducer.Reduce(state, new ZoomAction(10f));
            Assert.AreEqual(30f, result.TargetDistance, 0.001f);
        }

        [Test]
        public void CameraReducer_SpeedZoom_RespectsStateZoomLimits()
        {
            var state = CameraState.Default with
            {
                MinDistance = 10f, MaxDistance = 30f,
                MinZoomDistance = 12f, MaxZoomDistance = 28f
            };
            var result = CameraReducer.Reduce(state, new SpeedZoomAction(0f));
            // At speed 0, should be at MaxZoomDistance (28), clamped to [10, 30]
            Assert.AreEqual(28f, result.TargetDistance, 0.001f);
        }

        [Test]
        public void CameraReducer_FreeLook_RespectsStatePitchLimits()
        {
            var state = CameraState.Default with
            {
                FreeLookActive = true, MinPitch = -30f, MaxPitch = 30f, FreeLookPitch = 25f
            };
            var result = CameraReducer.Reduce(state, new FreeLookAction(0f, 10f));
            Assert.AreEqual(30f, result.FreeLookPitch, 0.001f);
        }
    }
}
