using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;

namespace VoidHarvest.Features.Procedural.Tests
{
    [TestFixture]
    public class AsteroidFieldValidationTests
    {
        private AsteroidFieldDefinition _field;
        private OreDefinition _validOre;

        [SetUp]
        public void SetUp()
        {
            _field = ScriptableObject.CreateInstance<AsteroidFieldDefinition>();
            _validOre = ScriptableObject.CreateInstance<OreDefinition>();
            _validOre.OreId = "test_ore";

            _field.AsteroidCount = 100;
            _field.FieldRadius = 500f;
            _field.AsteroidSizeMin = 1f;
            _field.AsteroidSizeMax = 5f;
            _field.RotationSpeedMin = 0f;
            _field.RotationSpeedMax = 10f;
            _field.MinScaleFraction = 0.3f;
            _field.OreEntries = new[]
            {
                new OreFieldEntry
                {
                    OreDefinition = _validOre,
                    Weight = 1f
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_field);
            Object.DestroyImmediate(_validOre);
        }

        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            CallOnValidate(_field);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnValidate_AsteroidCountZero_LogsWarning()
        {
            _field.AsteroidCount = 0;
            LogAssert.Expect(LogType.Warning, new Regex("AsteroidCount"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_FieldRadiusZero_LogsWarning()
        {
            _field.FieldRadius = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("FieldRadius"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_SizeMaxLessThanSizeMin_LogsWarning()
        {
            _field.AsteroidSizeMin = 5f;
            _field.AsteroidSizeMax = 1f;
            LogAssert.Expect(LogType.Warning, new Regex("AsteroidSizeMax"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_RotationMaxLessThanRotationMin_LogsWarning()
        {
            _field.RotationSpeedMin = 10f;
            _field.RotationSpeedMax = 1f;
            LogAssert.Expect(LogType.Warning, new Regex("RotationSpeedMax"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_MinScaleFractionBelowMinimum_LogsWarning()
        {
            _field.MinScaleFraction = 0.05f;
            LogAssert.Expect(LogType.Warning, new Regex("MinScaleFraction"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_EmptyOreEntries_LogsWarning()
        {
            _field.OreEntries = System.Array.Empty<OreFieldEntry>();
            LogAssert.Expect(LogType.Warning, new Regex("OreEntries"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_NullOreDefinitionInEntry_LogsWarning()
        {
            _field.OreEntries = new[]
            {
                new OreFieldEntry
                {
                    OreDefinition = null,
                    Weight = 1f
                }
            };
            LogAssert.Expect(LogType.Warning, new Regex("OreDefinition"));
            CallOnValidate(_field);
        }

        [Test]
        public void OnValidate_ZeroWeightInEntry_LogsWarning()
        {
            _field.OreEntries = new[]
            {
                new OreFieldEntry
                {
                    OreDefinition = _validOre,
                    Weight = 0f
                }
            };
            LogAssert.Expect(LogType.Warning, new Regex("Weight"));
            CallOnValidate(_field);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(AsteroidFieldDefinition field)
        {
            var method = typeof(AsteroidFieldDefinition).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "AsteroidFieldDefinition must have an OnValidate method");
            method.Invoke(field, null);
        }
    }
}
