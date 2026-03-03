using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Ship.Data;

namespace VoidHarvest.Features.Ship.Tests
{
    [TestFixture]
    public class ShipArchetypeConfigValidationTests
    {
        private ShipArchetypeConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ShipArchetypeConfig>();
            _config.ArchetypeId = "test";
            _config.DisplayName = "Test Ship";
            _config.Mass = 100f;
            _config.MaxThrust = 500f;
            _config.MaxSpeed = 100f;
            _config.RotationTorque = 50f;
            _config.LinearDamping = 0.5f;
            _config.AngularDamping = 0.5f;
            _config.MiningPower = 10f;
            _config.ModuleSlots = 3;
            _config.CargoCapacity = 100f;
            _config.BaseLockTime = 1.5f;
            _config.MaxTargetLocks = 3;
            _config.MaxLockRange = 5000f;
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
        public void OnValidate_MassZero_LogsWarning()
        {
            _config.Mass = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("Mass"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MaxThrustZero_LogsWarning()
        {
            _config.MaxThrust = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("MaxThrust"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_CargoCapacityZero_LogsWarning()
        {
            _config.CargoCapacity = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("CargoCapacity"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_BaseLockTimeZero_LogsWarning()
        {
            _config.BaseLockTime = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("BaseLockTime"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_MaxTargetLocksZero_LogsWarning()
        {
            _config.MaxTargetLocks = 0;
            LogAssert.Expect(LogType.Warning, new Regex("MaxTargetLocks"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_ArchetypeIdEmpty_LogsWarning()
        {
            _config.ArchetypeId = "";
            LogAssert.Expect(LogType.Warning, new Regex("ArchetypeId"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DisplayNameEmpty_LogsWarning()
        {
            _config.DisplayName = "";
            LogAssert.Expect(LogType.Warning, new Regex("DisplayName"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_LinearDampingNegative_LogsWarning()
        {
            _config.LinearDamping = -1f;
            LogAssert.Expect(LogType.Warning, new Regex("LinearDamping"));
            CallOnValidate(_config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(ShipArchetypeConfig config)
        {
            var method = typeof(ShipArchetypeConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "ShipArchetypeConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
