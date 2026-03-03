using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class StationServicesConfigValidationTests
    {
        private StationServicesConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<StationServicesConfig>();
            _config.MaxConcurrentRefiningSlots = 3;
            _config.RefiningSpeedMultiplier = 1f;
            _config.RepairCostPerHP = 100;
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
        public void OnValidate_MaxConcurrentRefiningSlotsZero_LogsWarning()
        {
            _config.MaxConcurrentRefiningSlots = 0;
            LogAssert.Expect(LogType.Warning, new Regex("MaxConcurrentRefiningSlots"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_RefiningSpeedMultiplierZero_LogsWarning()
        {
            _config.RefiningSpeedMultiplier = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("RefiningSpeedMultiplier"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_RepairCostPerHPNegative_LogsWarning()
        {
            _config.RepairCostPerHP = -1;
            LogAssert.Expect(LogType.Warning, new Regex("RepairCostPerHP"));
            CallOnValidate(_config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(StationServicesConfig config)
        {
            var method = typeof(StationServicesConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "StationServicesConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
