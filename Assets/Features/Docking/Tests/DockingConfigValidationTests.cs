using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Docking.Data;

namespace VoidHarvest.Features.Docking.Tests
{
    [TestFixture]
    public class DockingConfigValidationTests
    {
        private DockingConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DockingConfig>();
            _config.MaxDockingRange = 500f;
            _config.SnapRange = 30f;
            _config.SnapDuration = 1.5f;
            _config.UndockClearanceDistance = 100f;
            _config.UndockDuration = 2f;
            _config.ApproachTimeout = 120f;
            _config.AlignTimeout = 30f;
            _config.AlignDotThreshold = 0.999f;
            _config.AlignAngVelThreshold = 0.01f;
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
        public void OnValidate_MaxDockingRangeZero_LogsWarning()
        {
            _config.MaxDockingRange = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("MaxDockingRange"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_SnapRangeZero_LogsWarning()
        {
            _config.SnapRange = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("SnapRange"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_SnapRangeGreaterThanOrEqualToMaxDockingRange_LogsWarning()
        {
            _config.SnapRange = 500f;
            _config.MaxDockingRange = 500f;
            LogAssert.Expect(LogType.Warning, new Regex("SnapRange"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_SnapDurationZero_LogsWarning()
        {
            _config.SnapDuration = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("SnapDuration"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_UndockClearanceDistanceZero_LogsWarning()
        {
            _config.UndockClearanceDistance = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("UndockClearanceDistance"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_UndockDurationZero_LogsWarning()
        {
            _config.UndockDuration = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("UndockDuration"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_ApproachTimeoutZero_LogsWarning()
        {
            _config.ApproachTimeout = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("ApproachTimeout"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_AlignTimeoutZero_LogsWarning()
        {
            _config.AlignTimeout = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("AlignTimeout"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_AlignDotThresholdZero_LogsWarning()
        {
            _config.AlignDotThreshold = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("AlignDotThreshold"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_AlignDotThresholdAboveOne_LogsWarning()
        {
            _config.AlignDotThreshold = 1.5f;
            LogAssert.Expect(LogType.Warning, new Regex("AlignDotThreshold"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_AlignAngVelThresholdZero_LogsWarning()
        {
            _config.AlignAngVelThreshold = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("AlignAngVelThreshold"));
            CallOnValidate(_config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(DockingConfig config)
        {
            var method = typeof(DockingConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "DockingConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
