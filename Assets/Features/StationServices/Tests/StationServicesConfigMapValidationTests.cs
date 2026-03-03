using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class StationServicesConfigMapValidationTests
    {
        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            var config = ScriptableObject.CreateInstance<StationServicesConfigMap>();
            var serviceConfig = ScriptableObject.CreateInstance<StationServicesConfig>();
            config.Bindings = new[]
            {
                new StationServicesConfigMap.StationServiceBinding
                {
                    StationId = 1,
                    Config = serviceConfig
                }
            };

            CallOnValidate(config);
            LogAssert.NoUnexpectedReceived();

            Object.DestroyImmediate(serviceConfig);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void OnValidate_DuplicateStationIds_LogsWarning()
        {
            var config = ScriptableObject.CreateInstance<StationServicesConfigMap>();
            var serviceConfig1 = ScriptableObject.CreateInstance<StationServicesConfig>();
            var serviceConfig2 = ScriptableObject.CreateInstance<StationServicesConfig>();
            config.Bindings = new[]
            {
                new StationServicesConfigMap.StationServiceBinding
                {
                    StationId = 1,
                    Config = serviceConfig1
                },
                new StationServicesConfigMap.StationServiceBinding
                {
                    StationId = 1,
                    Config = serviceConfig2
                }
            };

            LogAssert.Expect(LogType.Warning, new Regex("Duplicate StationId"));
            CallOnValidate(config);

            Object.DestroyImmediate(serviceConfig1);
            Object.DestroyImmediate(serviceConfig2);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void OnValidate_NullConfig_LogsWarning()
        {
            var config = ScriptableObject.CreateInstance<StationServicesConfigMap>();
            config.Bindings = new[]
            {
                new StationServicesConfigMap.StationServiceBinding
                {
                    StationId = 1,
                    Config = null
                }
            };

            LogAssert.Expect(LogType.Warning, new Regex("Config"));
            CallOnValidate(config);

            Object.DestroyImmediate(config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(StationServicesConfigMap config)
        {
            var method = typeof(StationServicesConfigMap).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "StationServicesConfigMap must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
