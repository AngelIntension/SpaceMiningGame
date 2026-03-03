using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Resources.Data;

namespace VoidHarvest.Features.Resources.Tests
{
    [TestFixture]
    public class RawMaterialValidationTests
    {
        private RawMaterialDefinition _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RawMaterialDefinition>();
            _config.MaterialId = "test_mat";
            _config.DisplayName = "Test Material";
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
        public void OnValidate_MaterialIdEmpty_LogsWarning()
        {
            _config.MaterialId = "";
            LogAssert.Expect(LogType.Warning, new Regex("MaterialId"));
            CallOnValidate(_config);
        }

        [Test]
        public void OnValidate_DisplayNameEmpty_LogsWarning()
        {
            _config.DisplayName = "";
            LogAssert.Expect(LogType.Warning, new Regex("DisplayName"));
            CallOnValidate(_config);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(RawMaterialDefinition config)
        {
            var method = typeof(RawMaterialDefinition).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "RawMaterialDefinition must have an OnValidate method");
            method.Invoke(config, null);
        }
    }
}
