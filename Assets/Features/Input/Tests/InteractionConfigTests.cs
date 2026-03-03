using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Input.Data;

namespace VoidHarvest.Features.Input.Tests
{
    [TestFixture]
    public class InteractionConfigValidationTests
    {
        private InteractionConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<InteractionConfig>();
            _config.DoubleClickWindow = 0.3f;
            _config.RadialMenuDragThreshold = 5f;
            _config.DefaultApproachDistance = 50f;
            _config.DefaultOrbitDistance = 100f;
            _config.DefaultKeepAtRangeDistance = 50f;
            _config.MiningBeamMaxRange = 50f;
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
        public void OnValidate_DoubleClickWindowBelowMin_LogsWarning()
        {
            _config.DoubleClickWindow = 0.05f;
            LogAssert.Expect(LogType.Warning, new Regex("DoubleClickWindow"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DoubleClickWindowAboveMax_LogsWarning()
        {
            _config.DoubleClickWindow = 1.5f;
            LogAssert.Expect(LogType.Warning, new Regex("DoubleClickWindow"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_RadialMenuDragThresholdBelowMin_LogsWarning()
        {
            _config.RadialMenuDragThreshold = 0.5f;
            LogAssert.Expect(LogType.Warning, new Regex("RadialMenuDragThreshold"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_RadialMenuDragThresholdAboveMax_LogsWarning()
        {
            _config.RadialMenuDragThreshold = 25f;
            LogAssert.Expect(LogType.Warning, new Regex("RadialMenuDragThreshold"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DefaultApproachDistanceZero_LogsWarning()
        {
            _config.DefaultApproachDistance = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("DefaultApproachDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DefaultOrbitDistanceZero_LogsWarning()
        {
            _config.DefaultOrbitDistance = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("DefaultOrbitDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DefaultKeepAtRangeDistanceZero_LogsWarning()
        {
            _config.DefaultKeepAtRangeDistance = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("DefaultKeepAtRangeDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MiningBeamMaxRangeZero_LogsWarning()
        {
            _config.MiningBeamMaxRange = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("MiningBeamMaxRange"));
            CallOnValidate(_config);
        }

        private static void CallOnValidate(InteractionConfig config)
        {
            var method = typeof(InteractionConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "InteractionConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
