using NUnit.Framework;
using UnityEngine;
using VoidHarvest.Features.Mining.Data;
using VoidHarvest.Features.Procedural.Data;

namespace VoidHarvest.Features.Procedural.Tests
{
    [TestFixture]
    public class WeightNormalizationTests
    {
        private OreDefinition CreateOre(string id)
        {
            var ore = ScriptableObject.CreateInstance<OreDefinition>();
            ore.OreId = id;
            ore.BaseYieldPerSecond = 1f;
            ore.Hardness = 1f;
            ore.VolumePerUnit = 0.1f;
            return ore;
        }

        [Test]
        public void NormalizeWeights_ArbitraryWeights_NormalizesCorrectly()
        {
            var ore1 = CreateOre("a");
            var ore2 = CreateOre("b");
            var ore3 = CreateOre("c");

            var entries = new[]
            {
                new OreFieldEntry { OreDefinition = ore1, Weight = 7f },
                new OreFieldEntry { OreDefinition = ore2, Weight = 2f },
                new OreFieldEntry { OreDefinition = ore3, Weight = 1f }
            };

            var result = AsteroidFieldDefinition.NormalizeWeights(entries);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(0.7f, result[0], 0.001f);
            Assert.AreEqual(0.2f, result[1], 0.001f);
            Assert.AreEqual(0.1f, result[2], 0.001f);

            Object.DestroyImmediate(ore1);
            Object.DestroyImmediate(ore2);
            Object.DestroyImmediate(ore3);
        }

        [Test]
        public void NormalizeWeights_SingleEntry_NormalizesToOne()
        {
            var ore = CreateOre("solo");
            var entries = new[]
            {
                new OreFieldEntry { OreDefinition = ore, Weight = 5f }
            };

            var result = AsteroidFieldDefinition.NormalizeWeights(entries);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1.0f, result[0], 0.001f);

            Object.DestroyImmediate(ore);
        }

        [Test]
        public void NormalizeWeights_ZeroWeightEntries_Excluded()
        {
            var ore1 = CreateOre("a");
            var ore2 = CreateOre("b");

            var entries = new[]
            {
                new OreFieldEntry { OreDefinition = ore1, Weight = 5f },
                new OreFieldEntry { OreDefinition = ore2, Weight = 0f }
            };

            var result = AsteroidFieldDefinition.NormalizeWeights(entries);

            // Zero-weight entry gets 0 normalized weight
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1.0f, result[0], 0.001f);
            Assert.AreEqual(0.0f, result[1], 0.001f);

            Object.DestroyImmediate(ore1);
            Object.DestroyImmediate(ore2);
        }

        [Test]
        public void NormalizeWeights_AllZeroWeight_ReturnsEmpty()
        {
            var ore1 = CreateOre("a");
            var ore2 = CreateOre("b");

            var entries = new[]
            {
                new OreFieldEntry { OreDefinition = ore1, Weight = 0f },
                new OreFieldEntry { OreDefinition = ore2, Weight = 0f }
            };

            var result = AsteroidFieldDefinition.NormalizeWeights(entries);

            Assert.AreEqual(0, result.Length);

            Object.DestroyImmediate(ore1);
            Object.DestroyImmediate(ore2);
        }

        [Test]
        public void NormalizeWeights_NullOreDefinitionEntries_Skipped()
        {
            var ore1 = CreateOre("a");

            var entries = new[]
            {
                new OreFieldEntry { OreDefinition = ore1, Weight = 5f },
                new OreFieldEntry { OreDefinition = null, Weight = 3f }
            };

            var result = AsteroidFieldDefinition.NormalizeWeights(entries);

            // Null entry gets 0 normalized weight
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1.0f, result[0], 0.001f);
            Assert.AreEqual(0.0f, result[1], 0.001f);

            Object.DestroyImmediate(ore1);
        }
    }

    [TestFixture]
    public class AsteroidFieldDefinitionValidationTests
    {
        private AsteroidFieldDefinition _fieldDef;

        [SetUp]
        public void SetUp()
        {
            _fieldDef = ScriptableObject.CreateInstance<AsteroidFieldDefinition>();
            _fieldDef.FieldName = "Test Field";
            _fieldDef.AsteroidCount = 100;
            _fieldDef.FieldRadius = 500f;
            _fieldDef.AsteroidSizeMin = 3f;
            _fieldDef.AsteroidSizeMax = 5f;
            _fieldDef.Seed = 42;
            _fieldDef.MinScaleFraction = 0.3f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fieldDef);
        }

        [Test]
        public void AsteroidFieldDefinition_IsScriptableObject()
        {
            Assert.IsInstanceOf<ScriptableObject>(_fieldDef);
        }

        [Test]
        public void AsteroidFieldDefinition_HasCreateAssetMenuAttribute()
        {
            var attrs = typeof(AsteroidFieldDefinition).GetCustomAttributes(
                typeof(CreateAssetMenuAttribute), false);
            Assert.AreEqual(1, attrs.Length);
            var attr = (CreateAssetMenuAttribute)attrs[0];
            Assert.AreEqual("VoidHarvest/Procedural/Asteroid Field Definition", attr.menuName);
        }

        [Test]
        public void AsteroidFieldDefinition_AsteroidCountPositive()
        {
            _fieldDef.AsteroidCount = 300;
            Assert.Greater(_fieldDef.AsteroidCount, 0);
        }

        [Test]
        public void AsteroidFieldDefinition_FieldRadiusPositive()
        {
            _fieldDef.FieldRadius = 2000f;
            Assert.Greater(_fieldDef.FieldRadius, 0f);
        }

        [Test]
        public void AsteroidFieldDefinition_SizeMinLessOrEqualSizeMax()
        {
            _fieldDef.AsteroidSizeMin = 3f;
            _fieldDef.AsteroidSizeMax = 5f;
            Assert.LessOrEqual(_fieldDef.AsteroidSizeMin, _fieldDef.AsteroidSizeMax);
        }

        [Test]
        public void AsteroidFieldDefinition_MinScaleFractionClamped()
        {
            // MinScaleFraction should be clamped to [0.1, 0.5] via Range attribute
            Assert.GreaterOrEqual(_fieldDef.MinScaleFraction, 0.1f);
            Assert.LessOrEqual(_fieldDef.MinScaleFraction, 0.5f);
        }
    }
}
