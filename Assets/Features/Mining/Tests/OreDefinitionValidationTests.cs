using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Resources.Data;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class OreDefinitionValidationTests
    {
        private OreDefinition _ore;

        [SetUp]
        public void SetUp()
        {
            _ore = ScriptableObject.CreateInstance<OreDefinition>();
            _ore.OreId = "test";
            _ore.DisplayName = "Test Ore";
            _ore.BaseYieldPerSecond = 1f;
            _ore.Hardness = 1f;
            _ore.VolumePerUnit = 1f;
            _ore.BaseValue = 10;
            _ore.RarityWeight = 0.5f;
            _ore.BaseProcessingTimePerUnit = 5f;
            _ore.RefiningCreditCostPerUnit = 0;
            _ore.RefiningOutputs = System.Array.Empty<RefiningOutputEntry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ore);
        }

        [Test]
        public void OnValidate_ValidConfig_NoWarnings()
        {
            CallOnValidate(_ore);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnValidate_BaseYieldPerSecondZero_LogsWarning()
        {
            _ore.BaseYieldPerSecond = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("BaseYieldPerSecond"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_HardnessZero_LogsWarning()
        {
            _ore.Hardness = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("Hardness"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_VolumePerUnitZero_LogsWarning()
        {
            _ore.VolumePerUnit = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("VolumePerUnit"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_BaseValueNegative_LogsWarning()
        {
            _ore.BaseValue = -1;
            LogAssert.Expect(LogType.Warning, new Regex("BaseValue"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_RarityWeightAboveOne_LogsWarning()
        {
            _ore.RarityWeight = 2f;
            LogAssert.Expect(LogType.Warning, new Regex("RarityWeight"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_OreIdEmpty_LogsWarning()
        {
            _ore.OreId = "";
            LogAssert.Expect(LogType.Warning, new Regex("OreId"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_DisplayNameEmpty_LogsWarning()
        {
            _ore.DisplayName = "";
            LogAssert.Expect(LogType.Warning, new Regex("DisplayName"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_BaseProcessingTimePerUnitZero_LogsWarning()
        {
            _ore.BaseProcessingTimePerUnit = 0f;
            LogAssert.Expect(LogType.Warning, new Regex("BaseProcessingTimePerUnit"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_RefiningCreditCostPerUnitNegative_LogsWarning()
        {
            _ore.RefiningCreditCostPerUnit = -1;
            LogAssert.Expect(LogType.Warning, new Regex("RefiningCreditCostPerUnit"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_RefiningOutputs_NullMaterial_LogsWarning()
        {
            _ore.RefiningOutputs = new[]
            {
                new RefiningOutputEntry
                {
                    Material = null,
                    BaseYieldPerUnit = 1,
                    VarianceMin = 0,
                    VarianceMax = 1
                }
            };
            LogAssert.Expect(LogType.Warning, new Regex("Material"));
            CallOnValidate(_ore);
        }

        [Test]
        public void OnValidate_RefiningOutputs_BaseYieldPerUnitZero_LogsWarning()
        {
            var mat = ScriptableObject.CreateInstance<RawMaterialDefinition>();
            _ore.RefiningOutputs = new[]
            {
                new RefiningOutputEntry
                {
                    Material = mat,
                    BaseYieldPerUnit = 0,
                    VarianceMin = 0,
                    VarianceMax = 1
                }
            };
            LogAssert.Expect(LogType.Warning, new Regex("BaseYieldPerUnit"));
            CallOnValidate(_ore);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void OnValidate_RefiningOutputs_VarianceMinGreaterThanMax_LogsWarning()
        {
            var mat = ScriptableObject.CreateInstance<RawMaterialDefinition>();
            _ore.RefiningOutputs = new[]
            {
                new RefiningOutputEntry
                {
                    Material = mat,
                    BaseYieldPerUnit = 1,
                    VarianceMin = 5,
                    VarianceMax = 2
                }
            };
            LogAssert.Expect(LogType.Warning, new Regex("VarianceMin"));
            CallOnValidate(_ore);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void OnValidate_WarningFormat_StartsWithAssetName()
        {
            _ore.name = "TestOreAsset";
            _ore.BaseYieldPerSecond = 0f;
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[TestOreAsset\]"));
            CallOnValidate(_ore);
        }

        /// <summary>
        /// Invokes the private OnValidate method via reflection.
        /// </summary>
        private static void CallOnValidate(OreDefinition ore)
        {
            var method = typeof(OreDefinition).GetMethod("OnValidate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "OreDefinition must have an OnValidate method");
            method.Invoke(ore, null);
        }
    }
}
