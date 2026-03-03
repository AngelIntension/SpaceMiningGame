using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Station.Data;

namespace VoidHarvest.Features.Station.Tests
{
    [TestFixture]
    public class StationDefinitionTests
    {
        private StationDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<StationDefinition>();
            _def.StationId = 1;
            _def.DisplayName = "Test Station";
            _def.StationType = StationType.MiningRelay;
            _def.AvailableServices = new[] { "Sell" };
            _def.ServicesConfig = ScriptableObject.CreateInstance<StationServicesConfig>();
            _def.DockingPortOffset = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            if (_def != null && _def.ServicesConfig != null)
                Object.DestroyImmediate(_def.ServicesConfig);
            if (_def != null)
                Object.DestroyImmediate(_def);
        }

        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            CallOnValidate(_def);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnValidate_StationIdZero_LogsWarning()
        {
            _def.StationId = 0;
            LogAssert.Expect(LogType.Warning, new Regex("StationId must be > 0"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_StationIdNegative_LogsWarning()
        {
            _def.StationId = -5;
            LogAssert.Expect(LogType.Warning, new Regex("StationId must be > 0"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_DisplayNameEmpty_LogsWarning()
        {
            _def.DisplayName = "";
            LogAssert.Expect(LogType.Warning, new Regex("DisplayName must not be empty"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_DisplayNameNull_LogsWarning()
        {
            _def.DisplayName = null;
            LogAssert.Expect(LogType.Warning, new Regex("DisplayName must not be empty"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_ServicesConfigNull_LogsWarning()
        {
            Object.DestroyImmediate(_def.ServicesConfig);
            _def.ServicesConfig = null;
            LogAssert.Expect(LogType.Warning, new Regex("ServicesConfig must not be null"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_AvailableServicesEmpty_LogsWarning()
        {
            _def.AvailableServices = new string[0];
            LogAssert.Expect(LogType.Warning, new Regex("AvailableServices must have at least one entry"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_AvailableServicesNull_LogsWarning()
        {
            _def.AvailableServices = null;
            LogAssert.Expect(LogType.Warning, new Regex("AvailableServices must have at least one entry"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_DockingPortOffsetTooLarge_LogsWarning()
        {
            _def.DockingPortOffset = new Vector3(200f, 0f, 0f);
            LogAssert.Expect(LogType.Warning, new Regex("DockingPortOffset magnitude must be < 200"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_DockingPortOffsetAtLimit_LogsWarning()
        {
            // magnitude == 200 is not < 200, so should warn
            _def.DockingPortOffset = new Vector3(0f, 200f, 0f);
            LogAssert.Expect(LogType.Warning, new Regex("DockingPortOffset magnitude must be < 200"));
            CallOnValidate(_def);
        }

        [Test]
        public void OnValidate_DockingPortOffsetJustUnderLimit_NoWarning()
        {
            _def.DockingPortOffset = new Vector3(199f, 0f, 0f);
            CallOnValidate(_def);
            LogAssert.NoUnexpectedReceived();
        }

        private static void CallOnValidate(StationDefinition def)
        {
            var method = typeof(StationDefinition).GetMethod("OnValidate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "StationDefinition must have an OnValidate method");
            method.Invoke(def, null);
        }
    }
}
