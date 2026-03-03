using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.StationServices.Data;

namespace VoidHarvest.Features.StationServices.Tests
{
    [TestFixture]
    public class GameServicesConfigValidationTests
    {
        private GameServicesConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameServicesConfig>();
            _config.StartingCredits = 0;
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
        public void OnValidate_StartingCreditsNegative_LogsWarning()
        {
            _config.StartingCredits = -1;
            LogAssert.Expect(LogType.Warning, new Regex("StartingCredits"));
            CallOnValidate(_config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(GameServicesConfig config)
        {
            var method = typeof(GameServicesConfig).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "GameServicesConfig must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
