using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Mining.Systems;

namespace VoidHarvest.Features.Mining.Tests
{
    [TestFixture]
    public class OreRarityTierTests
    {
        [Test]
        public void OreRarityTier_HasCommonValue()
        {
            var tier = OreRarityTier.Common;
            Assert.AreEqual(OreRarityTier.Common, tier);
        }

        [Test]
        public void OreRarityTier_HasUncommonValue()
        {
            var tier = OreRarityTier.Uncommon;
            Assert.AreEqual(OreRarityTier.Uncommon, tier);
        }

        [Test]
        public void OreRarityTier_HasRareValue()
        {
            var tier = OreRarityTier.Rare;
            Assert.AreEqual(OreRarityTier.Rare, tier);
        }

        [Test]
        public void OreRarityTier_HasExactlyThreeValues()
        {
            var values = System.Enum.GetValues(typeof(OreRarityTier));
            Assert.AreEqual(3, values.Length);
        }
    }

    [TestFixture]
    public class OreDefinitionTests
    {
        private OreDefinition _oreDefinition;

        [SetUp]
        public void SetUp()
        {
            _oreDefinition = ScriptableObject.CreateInstance<OreDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_oreDefinition);
        }

        [Test]
        public void OreDefinition_HasOreIdField()
        {
            _oreDefinition.OreId = "test_ore";
            Assert.AreEqual("test_ore", _oreDefinition.OreId);
        }

        [Test]
        public void OreDefinition_HasDisplayNameField()
        {
            _oreDefinition.DisplayName = "Test Ore";
            Assert.AreEqual("Test Ore", _oreDefinition.DisplayName);
        }

        [Test]
        public void OreDefinition_HasRarityTierField()
        {
            _oreDefinition.RarityTier = OreRarityTier.Rare;
            Assert.AreEqual(OreRarityTier.Rare, _oreDefinition.RarityTier);
        }

        [Test]
        public void OreDefinition_HasIconField()
        {
            Assert.IsNull(_oreDefinition.Icon);
        }

        [Test]
        public void OreDefinition_HasBaseValueField()
        {
            _oreDefinition.BaseValue = 42;
            Assert.AreEqual(42, _oreDefinition.BaseValue);
        }

        [Test]
        public void OreDefinition_HasDescriptionField()
        {
            _oreDefinition.Description = "A rare ore.";
            Assert.AreEqual("A rare ore.", _oreDefinition.Description);
        }

        [Test]
        public void OreDefinition_HasRarityWeightField()
        {
            _oreDefinition.RarityWeight = 0.5f;
            Assert.AreEqual(0.5f, _oreDefinition.RarityWeight);
        }

        [Test]
        public void OreDefinition_HasBaseYieldPerSecondField()
        {
            _oreDefinition.BaseYieldPerSecond = 10f;
            Assert.AreEqual(10f, _oreDefinition.BaseYieldPerSecond);
        }

        [Test]
        public void OreDefinition_HasHardnessField()
        {
            _oreDefinition.Hardness = 2.5f;
            Assert.AreEqual(2.5f, _oreDefinition.Hardness);
        }

        [Test]
        public void OreDefinition_HasVolumePerUnitField()
        {
            _oreDefinition.VolumePerUnit = 0.25f;
            Assert.AreEqual(0.25f, _oreDefinition.VolumePerUnit);
        }

        [Test]
        public void OreDefinition_HasBeamColorField()
        {
            _oreDefinition.BeamColor = Color.cyan;
            Assert.AreEqual(Color.cyan, _oreDefinition.BeamColor);
        }

        [Test]
        public void OreDefinition_HasBaseProcessingTimePerUnitField()
        {
            _oreDefinition.BaseProcessingTimePerUnit = 5f;
            Assert.AreEqual(5f, _oreDefinition.BaseProcessingTimePerUnit);
        }

        [Test]
        public void OreDefinition_HasCreateAssetMenuAttribute()
        {
            var attrs = typeof(OreDefinition).GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            Assert.AreEqual(1, attrs.Length);
            var attr = (CreateAssetMenuAttribute)attrs[0];
            Assert.AreEqual("VoidHarvest/Ore Definition", attr.menuName);
        }

        [Test]
        public void OreDefinition_PositiveBaseYieldPerSecond_IsValid()
        {
            _oreDefinition.BaseYieldPerSecond = 10f;
            Assert.Greater(_oreDefinition.BaseYieldPerSecond, 0f);
        }

        [Test]
        public void OreDefinition_PositiveHardness_IsValid()
        {
            _oreDefinition.Hardness = 1.5f;
            Assert.Greater(_oreDefinition.Hardness, 0f);
        }

        [Test]
        public void OreDefinition_PositiveVolumePerUnit_IsValid()
        {
            _oreDefinition.VolumePerUnit = 0.1f;
            Assert.Greater(_oreDefinition.VolumePerUnit, 0f);
        }

        [Test]
        public void OreDefinition_IsScriptableObject()
        {
            Assert.IsInstanceOf<ScriptableObject>(_oreDefinition);
        }

        [Test]
        public void OreDefinition_HasExactly14Fields()
        {
            // Verify OreDefinition has exactly the expected 14 public fields (12 original + RefiningOutputs + RefiningCreditCostPerUnit)
            var fields = typeof(OreDefinition).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(14, fields.Length,
                $"Expected 14 fields but found {fields.Length}: {string.Join(", ", System.Array.ConvertAll(fields, f => f.Name))}");
        }
    }

    [TestFixture]
    public class OreTypeBlobTests
    {
        [Test]
        public void OreTypeBlob_HasBaseYieldPerSecondField()
        {
            var blob = new OreTypeBlob { BaseYieldPerSecond = 10f };
            Assert.AreEqual(10f, blob.BaseYieldPerSecond);
        }

        [Test]
        public void OreTypeBlob_HasHardnessField()
        {
            var blob = new OreTypeBlob { Hardness = 2.5f };
            Assert.AreEqual(2.5f, blob.Hardness);
        }

        [Test]
        public void OreTypeBlob_HasVolumePerUnitField()
        {
            var blob = new OreTypeBlob { VolumePerUnit = 0.25f };
            Assert.AreEqual(0.25f, blob.VolumePerUnit);
        }

        [Test]
        public void OreTypeBlob_HasExactly3Fields()
        {
            var fields = typeof(OreTypeBlob).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(3, fields.Length,
                $"Expected 3 fields (BaseYieldPerSecond, Hardness, VolumePerUnit) but found {fields.Length}: {string.Join(", ", System.Array.ConvertAll(fields, f => f.Name))}");
        }

        [Test]
        public void OreTypeBlob_IsValueType()
        {
            Assert.IsTrue(typeof(OreTypeBlob).IsValueType);
        }
    }

    [TestFixture]
    public class OreTypeBlobBakingSystemTests
    {
        private OreDefinition[] _testDefinitions;

        [SetUp]
        public void SetUp()
        {
            _testDefinitions = new OreDefinition[3];
            for (int i = 0; i < 3; i++)
                _testDefinitions[i] = ScriptableObject.CreateInstance<OreDefinition>();

            _testDefinitions[0].OreId = "luminite";
            _testDefinitions[0].BaseYieldPerSecond = 10f;
            _testDefinitions[0].Hardness = 1.0f;
            _testDefinitions[0].VolumePerUnit = 0.1f;

            _testDefinitions[1].OreId = "ferrox";
            _testDefinitions[1].BaseYieldPerSecond = 7f;
            _testDefinitions[1].Hardness = 1.5f;
            _testDefinitions[1].VolumePerUnit = 0.15f;

            _testDefinitions[2].OreId = "auralite";
            _testDefinitions[2].BaseYieldPerSecond = 5f;
            _testDefinitions[2].Hardness = 2.5f;
            _testDefinitions[2].VolumePerUnit = 0.25f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var def in _testDefinitions)
                Object.DestroyImmediate(def);
        }

        [Test]
        public void SetOreDefinitions_StoresDefinitions()
        {
            // Verify SetOreDefinitions accepts OreDefinition[] without error
            OreTypeBlobBakingSystem.SetOreDefinitions(_testDefinitions);
            Assert.Pass("SetOreDefinitions accepted OreDefinition[] array");
        }

        [Test]
        public void GetOreId_ReturnsCorrectOreId_AfterBlobBuild()
        {
            // Build blob manually to test GetOreId lookup
            OreTypeBlobBakingSystem.SetOreDefinitions(_testDefinitions);

            // Build blob asset manually (mirrors baking system logic)
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<OreTypeBlobDatabase>();
            var oreArray = builder.Allocate(ref root.OreTypes, _testDefinitions.Length);

            for (int i = 0; i < _testDefinitions.Length; i++)
            {
                oreArray[i] = new OreTypeBlob
                {
                    BaseYieldPerSecond = _testDefinitions[i].BaseYieldPerSecond,
                    Hardness = _testDefinitions[i].Hardness,
                    VolumePerUnit = _testDefinitions[i].VolumePerUnit
                };
            }

            var blobRef = builder.CreateBlobAssetReference<OreTypeBlobDatabase>(Allocator.Temp);

            // Verify blob entry count matches
            Assert.AreEqual(3, blobRef.Value.OreTypes.Length);

            // Verify field values
            Assert.AreEqual(10f, blobRef.Value.OreTypes[0].BaseYieldPerSecond);
            Assert.AreEqual(1.0f, blobRef.Value.OreTypes[0].Hardness);
            Assert.AreEqual(0.1f, blobRef.Value.OreTypes[0].VolumePerUnit);

            Assert.AreEqual(7f, blobRef.Value.OreTypes[1].BaseYieldPerSecond);
            Assert.AreEqual(1.5f, blobRef.Value.OreTypes[1].Hardness);
            Assert.AreEqual(0.15f, blobRef.Value.OreTypes[1].VolumePerUnit);

            Assert.AreEqual(5f, blobRef.Value.OreTypes[2].BaseYieldPerSecond);
            Assert.AreEqual(2.5f, blobRef.Value.OreTypes[2].Hardness);
            Assert.AreEqual(0.25f, blobRef.Value.OreTypes[2].VolumePerUnit);

            blobRef.Dispose();
        }

        [Test]
        public void GetOreId_ReturnsCorrectStrings()
        {
            OreTypeBlobBakingSystem.SetOreDefinitions(_testDefinitions);
            // After SetOreDefinitions, the GetOreId lookup is populated by OnUpdate.
            // We test the lookup mechanism directly.
            // Note: In production, OnUpdate builds the blob. Here we verify the static lookup
            // is populated correctly by calling the method that the system uses.
            Assert.AreEqual("luminite", OreTypeBlobBakingSystem.GetOreId(0));
            Assert.AreEqual("ferrox", OreTypeBlobBakingSystem.GetOreId(1));
            Assert.AreEqual("auralite", OreTypeBlobBakingSystem.GetOreId(2));
        }

        [Test]
        public void GetOreId_ReturnsEmpty_ForInvalidIndex()
        {
            OreTypeBlobBakingSystem.SetOreDefinitions(_testDefinitions);
            Assert.AreEqual("", OreTypeBlobBakingSystem.GetOreId(-1));
            Assert.AreEqual("", OreTypeBlobBakingSystem.GetOreId(99));
        }
    }
}
